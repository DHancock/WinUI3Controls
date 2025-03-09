using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestSolution.GroupBoxTests
{
    internal partial class InheritedGroupBox : AssyntSoftware.WinUI3Controls.GroupBox
    {
        public InheritedGroupBox() : base()
        {
            BorderThickness = new Microsoft.UI.Xaml.Thickness(2);
        }
    }
}
