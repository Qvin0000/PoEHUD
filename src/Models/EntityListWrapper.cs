using PoeHUD.Controllers;
using PoeHUD.Poe;
using PoeHUD.Poe.Elements;
using System;
using System.Collections.Generic;
using PoeHUD.Framework;
using PoeHUD.Framework.Helpers;

namespace PoeHUD.Models
{
    public sealed class EntityListWrapper
    {
        private readonly GameController gameController;
        private readonly HashSet<string> ignoredEntities;
        private Dictionary<long, EntityWrapper> entityCache;
        Coroutine parallelUpdateDictionary;
        Coroutine updateEntity;
        public Dictionary<Enums.PlayerStats, int> PlayerStats { get; private set; } = new Dictionary<Enums.PlayerStats, int>();
        public EntityListWrapper(GameController gameController)
        {
            this.gameController = gameController;
            entityCache = new Dictionary<long, EntityWrapper>();
            ignoredEntities = new HashSet<string>();
            gameController.Area.OnAreaChange += OnAreaChanged;
            EntitiesVersion = 0;
            updateEntity = (new Coroutine(() => { RefreshState(); },new WaitTime(coroutineTimeWait), nameof(GameController), "Update Entity"){Priority = CoroutinePriority.High}).AutoRestart(gameController.CoroutineRunner).Run();
            parallelUpdateDictionary = (new Coroutine(() =>
                {

                    if (parallelDictUpdated) return;
                    newEntities = gameController.Game.IngameState.Data.EntityList.EntitiesAsDictionary;
                    parallelDictUpdated = true;

                }, new WaitTime(coroutineTimeWait), nameof(EntityListWrapper), "EntitiesAsDictionary") {Priority = CoroutinePriority.High})
                .AutoRestart(gameController.CoroutineRunnerParallel).RunParallel();
        }

        public void UpdateCondition()
        {
            coroutineTimeWait = gameController.Performance.timeUpdateEntity;
            parallelUpdateDictionary.UpdateCondtion(new WaitTime(coroutineTimeWait));
            updateEntity.UpdateCondtion(new WaitTime(coroutineTimeWait));
        }
        
        public ICollection<EntityWrapper> Entities => entityCache.Values;

        private EntityWrapper player;

        public EntityWrapper Player
        {
            get
            {
                if (player == null)
                    UpdatePlayer();
                return player;
            }
        }

        public event Action<EntityWrapper> EntityAdded;
        public event Action<EntityWrapper> EntityAddedAny = delegate { };
        public event Action<EntityWrapper> EntityRemoved;
        
        private void OnAreaChanged(AreaController area){ 
            
            ignoredEntities.Clear();
            RemoveOldEntitiesFromCache();
            UpdatePlayer();
        }

        private void RemoveOldEntitiesFromCache()
        {
            foreach (var current in Entities)
            {
                EntityRemoved?.Invoke(current);
                current.IsInList = false;
            }
            entityCache.Clear();
        }
        
        public int EntitiesVersion;
        private bool parallelDictUpdated = false;
        private int coroutineTimeWait = 100;
        Dictionary<int, Entity> newEntities = new Dictionary<int, Entity>();
        
        private void UpdatePlayerStats()
        {
            var stats = player.GetComponent<Poe.Components.Stats>();
            int key = 0;
            int value = 0;
            var bytes = gameController.Memory.ReadBytes(stats.statPtrStart, (int)(stats.statPtrEnd - stats.statPtrStart));
            for (int i = 0; i < bytes.Length; i += 8)
            {
                key = BitConverter.ToInt32(bytes, i);
                value = BitConverter.ToInt32(bytes, i + 0x04);
                if (value != 0)
                    PlayerStats[(Enums.PlayerStats)key] = value;
                else if (PlayerStats.ContainsKey((Enums.PlayerStats)key))
                    PlayerStats.Remove((Enums.PlayerStats)key);
            }
        }
        public void RefreshState()
        {
            if (gameController.Area.CurrentArea == null)
                return;
            if(player.IsAlive && player.IsValid)
            UpdatePlayerStats();
            
            if (!parallelDictUpdated) return;
            var newCache = new Dictionary<long, EntityWrapper>();
            foreach (var keyEntity in newEntities)
            {
                long entityID = keyEntity.Key;
                string uniqueEntityName = keyEntity.Value.Path + entityID;

                if (ignoredEntities.Contains(uniqueEntityName))
                    continue;

                if (entityCache.ContainsKey(entityID) && entityCache[entityID].IsValid)
                {
                    newCache.Add(entityID, entityCache[entityID]);
                    entityCache[entityID].IsInList = true;
                    entityCache.Remove(entityID);
                    continue;
                }
                var entity = new EntityWrapper(gameController, keyEntity.Value);
                EntityAddedAny(entity);
                if (entity.Path.StartsWith("Metadata/Effects") || ((entityID & 0x80000000L) != 0L) ||
                    entity.Path.StartsWith("Metadata/Monsters/Daemon"))
                {
                    ignoredEntities.Add(uniqueEntityName);
                    continue;
                }
                EntityAdded?.Invoke(entity);
                newCache.Add(entityID, entity);
            }
            RemoveOldEntitiesFromCache();
            entityCache = newCache;
            parallelDictUpdated = false;
            EntitiesVersion++;
        }

        private void UpdatePlayer()
        {
            long address = gameController.Game.IngameState.Data.LocalPlayer.Address;
            if ((player == null) || (player.Address != address))
            {
                player = new EntityWrapper(gameController, address);
            }
            if (player.IsAlive && player.IsValid && player.HasComponent<Poe.Components.Stats>())
                          {
                               var stats = player.GetComponent<Poe.Components.Stats>();
                                int key = 0;
                              int value = 0;
                              var bytes = gameController.Memory.ReadBytes(stats.statPtrStart, (int) (stats.statPtrEnd - stats.statPtrStart));
                              for (int i = 0; i < bytes.Length; i += 8)
                                  PlayerStats[(Enums.PlayerStats) BitConverter.ToInt32(bytes, i)] = BitConverter.ToInt32(bytes, i + 0x04);
                           }
        }

        public EntityWrapper GetEntityById(long id)
        {
            EntityWrapper result;
            return entityCache.TryGetValue(id, out result) ? result : null;
        }

        public EntityLabel GetLabelForEntity(Entity entity)
        {
            var hashSet = new HashSet<long>();
            long entityLabelMap = gameController.Game.IngameState.EntityLabelMap;
            long num = entityLabelMap;
            
            while (true)
            {
                hashSet.Add(num);
                if (gameController.Memory.ReadLong(num + 0x10) == entity.Address)
                {
                    break;
                }
                num = gameController.Memory.ReadLong(num);
                if (hashSet.Contains(num) || num == 0 || num == -1)
                {
                    return null;
                }
            }
            return gameController.Game.ReadObject<EntityLabel>(num + 0x18);
        }
    }
}