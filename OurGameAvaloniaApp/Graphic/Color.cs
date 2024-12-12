using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Graphic
{
    public readonly struct Color
    {
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }
        public byte A { get; }
        public Color(byte R, byte G, byte B, byte A) { this.R = R; this.G = G; this.B = B; this.A = A; }
    }
}
