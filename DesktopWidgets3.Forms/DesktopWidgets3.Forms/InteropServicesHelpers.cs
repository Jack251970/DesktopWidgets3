using System.Runtime.InteropServices.ComTypes;

namespace DesktopWidgets3.Forms
{
    public static class InteropServicesHelpers
    {
        public static DVASPECT DVASPECT_CONTENT => DVASPECT.DVASPECT_CONTENT;

        public interface InteropIStream : IStream {}
    }
}
