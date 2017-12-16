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

        public EntityListWrapper(GameController gameController)
        {
            this.gameController = gameController;
            entityCache = new Dictionary<long, EntityWrapper>();
            ignoredEntities = new HashSet<string>();
            gameController.Area.OnAreaChange += OnAreaChanged;
            updateEntity = (new Coroutine(() => { RefreshState(); },new WaitTime(coroutineTimeWait), nameof(GameController), "Update Entity"){Priority = CoroutinePriority.High}).AutoRestart(gameController.CoroutineRunner).Run();
            parallelUpdateDictionary = (new Coroutine(() =>
                {

                    if (parallelDictUpdated) return;
                    newEntities = gameController.Game.IngameState.Data.EntityList.EntitiesAsDictionary;
                    parallelDictUpdated = true;

                }, new WaitTime(coroutineTimeWait), nameof(EntityListWrapper), "EntitiesAsDictionary") {Priority = CoroutinePriority.High})
                .AutoRestart(gameController.CoroutineRunnerParallel).RunParallel();
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

        public event Action<EntityWrapper> EntityRemoved;
        private bool _once = false;
        private void OnAreaChanged(AreaController area)
        { 
            
            if (!_once)
            {
                coroutineTimeWait = 1000 / this.gameController.Performance.UpdateEntityDataLimit;
                gameController.Performance.UpdateEntityDataLimit.OnValueChanged += () =>
                {
                    coroutineTimeWait = 1000 / this.gameController.Performance.UpdateEntityDataLimit;
                    parallelUpdateDictionary.UpdateCondtion(new WaitTime(coroutineTimeWait));
                    updateEntity.UpdateCondtion(new WaitTime(coroutineTimeWait));
                };
                _once = true;
            }
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

        public bool AllEntitiesUpdated = false;
        private bool parallelDictUpdated = false;
        private int coroutineTimeWait = 100;
        Dictionary<int, Entity> newEntities = new Dictionary<int, Entity>();
        
  
        public void RefreshState()
        {
            if (gameController.Area.CurrentArea == null)
                return;
            if (!parallelDictUpdated) return;
            AllEntitiesUpdated = false;
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
            AllEntitiesUpdated = true;
        }

        private void UpdatePlayer()
        {
            long address = gameController.Game.IngameState.Data.LocalPlayer.Address;
            if ((player == null) || (player.Address != address))
            {
                player = new EntityWrapper(gameController, address);
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