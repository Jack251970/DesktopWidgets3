using System.Collections.Specialized;
using System.Windows.Forms;

namespace DesktopWidgets3.Forms
{
    public static class FormsHelpers
    {
        public static void SetClipboard(string[] filesToCopy, object dropEffect)
        {
            Clipboard.Clear();
            var fileList = new StringCollection();
            fileList.AddRange(filesToCopy);
            var data = new DataObject();
            data.SetFileDropList(fileList);
            data.SetData("Preferred DropEffect", dropEffect);
            Clipboard.SetDataObject(data, true);
        }
    }
}
