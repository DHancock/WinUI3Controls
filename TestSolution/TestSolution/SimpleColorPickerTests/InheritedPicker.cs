using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI;

namespace TestSolution.SimpleColorPickerTests
{
    internal class InheritedPicker : AssyntSoftware.WinUI3Controls.SimpleColorPicker
    {
        public InheritedPicker() : base()
        {
            Palette = SimpleColorPickerPage.BuildRandomPalette();
            CellsPerColumn = 3;
        }
    }
}
