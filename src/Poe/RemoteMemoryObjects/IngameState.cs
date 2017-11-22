using PoeHUD.Models.Enums;
using System;
using PoeHUD.Models;

namespace PoeHUD.Poe.RemoteMemoryObjects
{
    public class IngameState : RemoteMemoryObject
    {
        public Camera Camera => Cache.Enable ? Cache.Instance.Camera : CameraReal;

        public Camera CameraReal => GetObject<Camera>(Address + 0x1704 + Offsets.IgsOffsetDelta);

        public IngameData Data => Cache.Enable ? Cache.Instance.Data : DataReal;
        public IngameData DataReal => ReadObject<IngameData>(Address + 0x170 + Offsets.IgsOffset);

        public bool InGame => ServerDataReal.IsInGame;
        public ServerData ServerData => Cache.Enable ? Cache.Instance.ServerData : ServerDataReal;

        public ServerData ServerDataReal => ReadObjectAt<ServerData>(0x178 + Offsets.IgsOffset);
        public IngameUIElements IngameUi => Cache.Enable ? Cache.Instance.IngameUi : IngameUiReal;

        public IngameUIElements IngameUiReal => ReadObjectAt<IngameUIElements>(0x5D0 + Offsets.IgsOffset);
        public Element UIRoot => Cache.Enable ? Cache.Instance.UIRoot : UIRootReal;

        public Element UIRootReal => ReadObjectAt<Element>(0xC80 + Offsets.IgsOffset);
        public Element UIHover => ReadObjectAt<Element>(0xCA8 + Offsets.IgsOffset);

        public float CurentUIElementPosX => M.ReadFloat(Address + 0xCB0 + Offsets.IgsOffset);
        public float CurentUIElementPosY => M.ReadFloat(Address + 0xCB4 + Offsets.IgsOffset);

        public long EntityLabelMap => M.ReadLong(Address + 0x98, 0xA70);
        public DiagnosticInfoType DiagnosticInfoType => (DiagnosticInfoType)M.ReadInt(Address + 0xD38 + Offsets.IgsOffset);
        public DiagnosticElement LatencyRectangle => Cache.Enable ? Cache.Instance.LatencyRectangle : LatencyRectangleReal;

        public DiagnosticElement LatencyRectangleReal => GetObjectAt<DiagnosticElement>(0xF68 + Offsets.IgsOffset);
        public DiagnosticElement FrameTimeRectangle => GetObjectAt<DiagnosticElement>(0x13F8 + Offsets.IgsOffset);
        public DiagnosticElement FPSRectangle => Cache.Enable ? Cache.Instance.FPSRectangle : FPSRectangleReal;

        public DiagnosticElement FPSRectangleReal => GetObjectAt<DiagnosticElement>(0x1640 + Offsets.IgsOffset);
        public float CurLatency => LatencyRectangle.CurrValue;
        public float CurFrameTime => FrameTimeRectangle.CurrValue;
        public float CurFps => FPSRectangle.CurrValue;
        public TimeSpan TimeInGame => TimeSpan.FromSeconds(M.ReadFloat(Address + 0xD1C + Offsets.IgsOffset));
        public float TimeInGameF => M.ReadFloat(Address + 0xD20 + Offsets.IgsOffset);


    }
}