using PoeHUD.Models.Enums;
using System;
using PoeHUD.Controllers;
using PoeHUD.Models;
namespace PoeHUD.Poe.RemoteMemoryObjects
{
    public class IngameState : RemoteMemoryObject
    {
        private Cache Cache;
        public IngameState()
        {
            Cache = GameController.Instance.Cache;
        }

        public Camera Camera =>Cache.Enable && Cache.Camera!=null ? Cache.Camera : 
            Cache.Enable ? Cache.Camera = CameraReal: CameraReal;

        private Camera CameraReal => GetObject<Camera>(Address + 0x179C  + Offsets.IgsOffsetDelta);

        public IngameData Data =>Cache.Enable && Cache.Data!=null ? Cache.Data : Cache.Enable? Cache.Data = DataReal:  DataReal;
        private IngameData DataReal => ReadObject<IngameData>(Address + 0x1F0  + Offsets.IgsOffset);

        public bool InGame => ServerDataReal.IsInGame;
        public ServerData ServerData =>Cache.Enable && Cache.ServerData!=null ?Cache.ServerData : Cache.Enable? Cache.ServerData=ServerDataReal : ServerDataReal;

        private ServerData ServerDataReal => ReadObjectAt<ServerData>(0x1F8  + Offsets.IgsOffset);
        public IngameUIElements IngameUi =>Cache.Enable && Cache.IngameUi!=null ?Cache.IngameUi : Cache.Enable? Cache.IngameUi=IngameUiReal : IngameUiReal;

        private IngameUIElements IngameUiReal => ReadObjectAt<IngameUIElements>(0x650  + Offsets.IgsOffset);
        public Element UIRoot =>Cache.Enable && Cache.UIRoot!=null ?Cache.UIRoot : Cache.Enable ? Cache.UIRoot=UIRootReal: UIRootReal;

        private Element UIRootReal => ReadObjectAt<Element>(0xD00  + Offsets.IgsOffset);
        public Element UIHover => ReadObjectAt<Element>(0xD38  + Offsets.IgsOffset);

        public float CurentUIElementPosX => M.ReadFloat(Address + 0x930  + Offsets.IgsOffset);
        public float CurentUIElementPosY => M.ReadFloat(Address + 0x934  + Offsets.IgsOffset);

        public long EntityLabelMap => M.ReadLong(Address + 0x98, 0xA78);
        public DiagnosticInfoType DiagnosticInfoType => (DiagnosticInfoType)M.ReadInt(Address + 0xDD0  + Offsets.IgsOffset);
        public DiagnosticElement LatencyRectangle =>Cache.Enable && Cache.LatencyRectangle!=null ?Cache.LatencyRectangle : Cache.Enable ? Cache.LatencyRectangle=LatencyRectangleReal: LatencyRectangleReal;

        private DiagnosticElement LatencyRectangleReal => GetObjectAt<DiagnosticElement>(0x1000   + Offsets.IgsOffset);
        public DiagnosticElement FrameTimeRectangle => GetObjectAt<DiagnosticElement>(0x1490  + Offsets.IgsOffset);
        public DiagnosticElement FPSRectangle =>Cache.Enable && Cache.FPSRectangle!=null ?Cache.FPSRectangle : Cache.Enable? Cache.FPSRectangle=FPSRectangleReal: FPSRectangleReal;

        private DiagnosticElement FPSRectangleReal => GetObjectAt<DiagnosticElement>(0x16D8  + Offsets.IgsOffset);
        public float CurLatency => LatencyRectangle.CurrValue;
        public float CurFrameTime => FrameTimeRectangle.CurrValue;
        public float CurFps => FPSRectangle.CurrValue;
        public TimeSpan TimeInGame => TimeSpan.FromSeconds(M.ReadFloat(Address + 0xDB8  + Offsets.IgsOffset));
        public float TimeInGameF => M.ReadFloat(Address + 0xDBC  + Offsets.IgsOffset);


       
    }
}