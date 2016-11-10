using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using System.Data;
using System.Windows.Forms;

namespace LukeFileUpload
{
    class DB
    {
        private const String DB_FILE = "luke_uploadfile.db";

        private const String KEY_DB_VER = "db_version";
        private const String DB_VER = "1.0";       

        private SQLiteConnection mDbCon;


        public DB()
        {
            mDbCon = new SQLiteConnection("Data Source=" + DB_FILE + ";Version=3;");

            if (!File.Exists(DB_FILE) || String.IsNullOrEmpty(getDbVer()) || !getDbVer().Equals(DB_VER))
            {
                initDB();
            }
        }

        public void initDB()
        {
            File.Delete(DB_FILE);

            SQLiteConnection.CreateFile(DB_FILE);
            /*加入表*/

            String[] sqls = { "create table tb_appupload(id INTEGER Primary key AUTOINCREMENT,name char(50),description char(100),path char(255))",
                            "create table tb_config(id INTEGER Primary key AUTOINCREMENT,name char(100),val char(100))" ,
                           String.Format("insert into tb_config(name,val) values('{0}','{1}')",KEY_DB_VER,DB_VER)};

            for (int i = 0; i < sqls.Count(); i++)
            {
                update(sqls[i]);
            }
        }

        private String getDbVer()
        {
            String ver = null;
            string sql = String.Format("select val from tb_config where name='{0}'", KEY_DB_VER);
            DataSet ds = query(sql);
            if (ds.Tables[0].Rows.Count > 0)
            {
                ver = ds.Tables[0].Rows[0][0].ToString();
            }

            return ver;
        }

        public void update(String sql)
        {
            mDbCon.Open();

            SQLiteCommand command = new SQLiteCommand(sql, mDbCon);

            command.ExecuteNonQuery();

            mDbCon.Close();
        }

        public DataSet query(String sql)
        {

            mDbCon.Open();

            SQLiteCommand cmd = mDbCon.CreateCommand();


            cmd.CommandText = sql;
            DataSet ds = new DataSet();
            SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
            da.Fill(ds);
            da.Dispose();
            cmd.Dispose();

            mDbCon.Close();

            return ds;

        }
    }
}
