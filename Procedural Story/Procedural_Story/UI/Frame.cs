using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Procedural_Story.UI
{
    class Frame : UIElement
    {
        public Color Background;

        public Frame(UIElement parent, string name, UDim2 pos, UDim2 size, Color bg) : base(parent, name, pos, size) {
            Background = bg;
        }
    }
}
