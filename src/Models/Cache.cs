using System.Collections.Generic;
using PoeHUD.Controllers;
using PoeHUD.DebugPlug;
using PoeHUD.Framework;
using PoeHUD.Framework.Helpers;
using PoeHUD.Hud.Performance;
using PoeHUD.Models.CacheComponent;
using PoeHUD.Poe;
using PoeHUD.Poe.Components;
using PoeHUD.Poe.RemoteMemoryObjects;
using SharpDX;

namespace PoeHUD.Models
{
    public class Cache
    {
        private readonly GameController _gameController;
        private IngameState _ingameState = null;
        private Camera _camera;
        private Element _uiRoot;
        private IngameUIElements _ingameUi;
        private ServerData _serverData;
        private IngameData _data;
        private DiagnosticElement _fpsRectangle;
        private DiagnosticElement _latencyRectangle;
        private Element _questTracker;
        private Element _GemLvlUpPanel;
        private Entity _localPlayer;
        private RectangleF _window;
        private bool _enable = true;

        private static Cache _instance;

        public IngameState IngameState
        {
            get => _ingameState;
            set
            {
                if (_ingameState == null)
                    _ingameState = value;
            }
        }

        public Camera Camera
        {
            get => _camera;
            set
            {
                if(_camera==null)
                    _camera = value;
            }
        }

        public Element UIRoot
        {
            get => _uiRoot;
            set
            {
                if (_uiRoot == null)
                    _uiRoot = value;
            }
        }

        public IngameUIElements IngameUi
        {
            get => _ingameUi;
            set
            {
                if (_ingameUi == null)
                    _ingameUi = value;
            }
        }

        public ServerData ServerData
        {
            get => _serverData;
            set
            {
                if (_serverData==null)
                    _serverData = value;
            }
        }

        public IngameData Data
        {
            get => _data;
            set
            {
                if (_data == null)
                    _data = value;
            }
        }

        public DiagnosticElement FPSRectangle
        {
            get => _fpsRectangle;
            set
            {
                if (_fpsRectangle == null)
                    _fpsRectangle = value;
            }
        }

        public DiagnosticElement LatencyRectangle
        {
            get => _latencyRectangle;
            set
            {
                if (_latencyRectangle == null)
                    _latencyRectangle = value;
            }
        }
        public Element QuestTracker
        {    
            get => _questTracker;
            set
            {
                if (_questTracker == null)
                    _questTracker = value;
            }
        }
        public Element GemLvlUpPanel
        {
            get => _GemLvlUpPanel;
            set
            {
                if (_GemLvlUpPanel == null)
                    _GemLvlUpPanel = value;
            }
        }
        public Entity LocalPlayer
        {
            get => _localPlayer;
            set
            {
                if (_localPlayer == null)
                    _localPlayer = value;
            }
        }

        public RectangleF Window => _window.IsEmpty ? (_window= _gameController.Window.GetWindowRectangleReal()) :_window;


        public bool Enable
        {
            get { return _enable; }
            set
            {
                if (value)
                    UpdateCache();
                _enable = value;
            }
        }

        public Cache(GameController gameController)
        {
            _window = RectangleF.Empty;
            _gameController = gameController;
           
        }
        public void UpdateCache()
        {
            _gameController.Game.RefreshTheGameState();
            _ingameState = null;
            _camera = null;
            _uiRoot = null;
            _ingameUi = null;
            _serverData = null;
            UpdateDataCache();
            _fpsRectangle = null;
            _latencyRectangle = null;
            _localPlayer = null;
            _questTracker = null;
            _GemLvlUpPanel = null;
            _window = _gameController.Window.GetWindowRectangleReal();
            CacheElements.Clear();
            LittleCacheTime.Clear();
            LittleCache.Clear();
        }

        public void UpdateDataCache()
        {
            _data = null;
        }
        
        public void ForceUpdateWindowCache()
        {
            _window = _gameController.Window.GetWindowRectangleReal();
        }
        
       public readonly Dictionary<long,RemoteMemoryObject> CacheElements = new Dictionary<long, RemoteMemoryObject>(1024);
       public readonly Dictionary<long,float> LittleCacheTime = new Dictionary<long, float>(1024);
       public readonly Dictionary<long,object> LittleCache = new Dictionary<long, object>(1024);
    }
}