using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LukeFileUpload
{
    class UpdateInfo
    {
        public String version;
        public String appUrl;
        public String md5;
        public String size;
    }

    class UploadInfo
    {
        public String version;
        public String size;
        public String md5;
        public String id;
    }

    class ItemData
    {
        public String id;
        public String name;
        public String desUrl;
        public UpdateInfo updateInfo;

        public String locPath;
    }

    class ResData
    {
        public String result;
        public List<ItemData> data;
    }

    class SendRetData
    {
        public String result;
        public String description;

        public bool isSuccess()
        {
            if (!String.IsNullOrEmpty(result) && result.Equals("0"))
            {
                return true;
            }
            return false;
        }
    }
}
