using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LukeFileUpload
{
    class AppDao
    {
        private DB db = new DB();

        public AppDao()
        {
        }

        public String getAppPathById(int id)
        {
            DataSet ds = db.query("select path from tb_appupload where id = " + id);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return ds.Tables[0].Rows[0][0].ToString();
            }
            return null;
        }

        public void updateOrInsert(List<ItemData> list)
        {
            if (list != null && list.Count > 0)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    updateOrInsert(list[i]);
                }
            }
        }

        public void updateOrInsert(ItemData data)
        {
            if (data != null)
            {
                DataSet ds = db.query("select id from tb_appupload where id=" + data.id);

                if (ds.Tables[0].Rows.Count == 0)
                {                    
                    // 不存在
                    db.update(String.Format("insert into tb_appupload (id,name) values('{0}','{1}')", data.id, data.name));
                }
                else
                {
                    // 已经存在
                    String sql = "";
                    if (String.IsNullOrEmpty(data.locPath))
                    {
                        sql = String.Format("update tb_appupload set name='{0}' where id = {1}", data.name, data.id);
                    }
                    else
                    {
                        sql = String.Format("update tb_appupload set name='{0}',path='{1}' where id = {2}", data.name, data.locPath, data.id);
                    }

                    db.update(sql);
                }
            }
        }

        public void updateLocPath(int id, String path)
        {
            String sql = String.Format("update tb_appupload set path='{0}' where id ={1}", path, id);
            db.update(sql);
        }
    }
}
