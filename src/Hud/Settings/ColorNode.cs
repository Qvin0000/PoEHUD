using System;
using SharpDX;

namespace PoeHUD.Hud.Settings
{
    public sealed class ColorNode
    {
        private Color _value;

        public ColorNode()
        {
        }

        public ColorNode(uint color)
        {
            Value = Color.FromAbgr(color);
        }

        public ColorNode(Color color)
        {
            Value = color;
        }

        public Color Value
        {
            get => _value;
            set
            {
                if (!value.Equals(_value))
                {
                    _value = value;
                    try
                    {
                        OnValueChanged?.Invoke();
                    }
                    catch (Exception)
                    {

                        DebugPlug.DebugPlugin.LogMsg($"Error in function that subscribed for: {nameof(ColorNode)}.OnValueChanged", 10, SharpDX.Color.Red);
                    }
                }
            }
        }

        public event Action OnValueChanged;
        public static implicit operator Color(ColorNode node)
        {
            return node.Value;
        }

        public static implicit operator ColorNode(uint value)
        {
            return new ColorNode(value);
        }

        public static implicit operator ColorNode(Color value)
        {
            return new ColorNode(value);
        }

        public static implicit operator ColorNode(ColorBGRA value)
        {
            return new ColorNode(value);
        }
    }
}