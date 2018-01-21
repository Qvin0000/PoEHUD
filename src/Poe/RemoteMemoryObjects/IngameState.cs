using PoeHUD.Models.Enums;
using System;
using PoeHUD.Controllers;
using PoeHUD.Models;
namespace PoeHUD.Poe.RemoteMemoryObjects
{
    public class IngameState : RemoteMemoryObject
    {

        public Camera Camera =>Game.Performance.Cache.Enable && Game.Performance.Cache.Camera!=null ? Game.Performance.Cache.Camera : 
            Game.Performance.Cache.Enable ? Game.Performance.Cache.Camera = CameraReal: CameraReal;

        private Camera CameraReal => GetObject<Camera>(Address + 0x179C  + Offsets.IgsOffsetDelta);

        public IngameData Data =>Game.Performance.Cache.Enable && Game.Performance.Cache.Data!=null ? Game.Performance.Cache.Data : Game.Performance.Cache.Enable? Game.Performance.Cache.Data = DataReal:  DataReal;
        private IngameData DataReal => ReadObject<IngameData>(Address + 0x1F0  + Offsets.IgsOffset);

        public bool InGame => ServerDataReal.IsInGame;
        public ServerData ServerData =>Game.Performance.Cache.Enable && Game.Performance.Cache.ServerData!=null ?Game.Performance.Cache.ServerData : Game.Performance.Cache.Enable? Game.Performance.Cache.ServerData=ServerDataReal : ServerDataReal;

        private ServerData ServerDataReal => ReadObjectAt<ServerData>(0x1F8  + Offsets.IgsOffset);
        public IngameUIElements IngameUi =>Game.Performance.Cache.Enable && Game.Performance.Cache.IngameUi!=null ?Game.Performance.Cache.IngameUi : Game.Performance.Cache.Enable? Game.Performance.Cache.IngameUi=IngameUiReal : IngameUiReal;

        private IngameUIElements IngameUiReal => ReadObjectAt<IngameUIElements>(0x650  + Offsets.IgsOffset);
        public Element UIRoot =>Game.Performance.Cache.Enable && Game.Performance.Cache.UIRoot!=null ?Game.Performance.Cache.UIRoot : Game.Performance.Cache.Enable ? Game.Performance.Cache.UIRoot=UIRootReal: UIRootReal;

        private Element UIRootReal => ReadObjectAt<Element>(0xD00  + Offsets.IgsOffset);
        public Element UIHover => ReadObjectAt<Element>(0xD38  + Offsets.IgsOffset);

        public float CurentUIElementPosX => M.ReadFloat(Address + 0x930  + Offsets.IgsOffset);
        public float CurentUIElementPosY => M.ReadFloat(Address + 0x934  + Offsets.IgsOffset);

        public long EntityLabelMap => M.ReadLong(Address + 0x98, 0xA78);
        public DiagnosticInfoType DiagnosticInfoType => (DiagnosticInfoType)M.ReadInt(Address + 0xDD0  + Offsets.IgsOffset);
        public DiagnosticElement LatencyRectangle =>Game.Performance.Cache.Enable && Game.Performance.Cache.LatencyRectangle!=null ?Game.Performance.Cache.LatencyRectangle : Game.Performance.Cache.Enable ? Game.Performance.Cache.LatencyRectangle=LatencyRectangleReal: LatencyRectangleReal;

        private DiagnosticElement LatencyRectangleReal => GetObjectAt<DiagnosticElement>(0x1000   + Offsets.IgsOffset);
        public DiagnosticElement FrameTimeRectangle => GetObjectAt<DiagnosticElement>(0x1490  + Offsets.IgsOffset);
        public DiagnosticElement FPSRectangle =>Game.Performance.Cache.Enable && Game.Performance.Cache.FPSRectangle!=null ?Game.Performance.Cache.FPSRectangle : Game.Performance.Cache.Enable? Game.Performance.Cache.FPSRectangle=FPSRectangleReal: FPSRectangleReal;

        private DiagnosticElement FPSRectangleReal => GetObjectAt<DiagnosticElement>(0x16D8  + Offsets.IgsOffset);
        public float CurLatency => LatencyRectangle.CurrValue;
        public float CurFrameTime => FrameTimeRectangle.CurrValue;
        public float CurFps => FPSRectangle.CurrValue;
        public TimeSpan TimeInGame => TimeSpan.FromSeconds(M.ReadFloat(Address + 0xDB8  + Offsets.IgsOffset));
        public float TimeInGameF => M.ReadFloat(Address + 0xDBC  + Offsets.IgsOffset);


       
    }
}