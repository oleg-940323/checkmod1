﻿using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace checkmod
{
    /// <summary>
    /// РАбота с Базой данных
    /// </summary>
    class WorkWithBD
    {
        // Команда для базы данных
        string CommandText;

        // Имя файла с базой данных
        string str = "Data Source=BDParameters.db";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"> Экземпляр модуля </param>
        public void WorkBD(Module t)
        {
            // Обнуление команды
            CommandText = null;

            // Проверка имени модуля
            if (t.mod.Parameters[1].Parameter[1].Value.Text[0].StartsWith("TD9"))
            {
                CommandText = "SELECT * FROM " + t.mod.Parameters[1].Parameter[1].Value.Text[0].Substring(0, 3) + $" WHERE ID = '{t.pr.ident_collect[(byte)SeqIdentDataInCollect.plantnum].value.Trim('\'', '\"')}'";
            }
            else if (t.mod.Parameters[1].Parameter[1].Value.Text[0].StartsWith("TA901"))
            {
                CommandText = "SELECT * FROM " + t.mod.Parameters[1].Parameter[1].Value.Text[0] + $" WHERE ID = '{t.pr.ident_collect[(byte)SeqIdentDataInCollect.plantnum].value.Trim('\'', '\"')}'";
            }
            else if (t.mod.Parameters[1].Parameter[1].Value.Text[0].StartsWith("TA902"))
            {
                CommandText = "SELECT * FROM " + t.mod.Parameters[1].Parameter[1].Value.Text[0] + $" WHERE ID = '{t.pr.ident_collect[(byte)SeqIdentDataInCollect.plantnum].value.Trim('\'', '\"')}'";
            }
            else if (t.mod.Parameters[1].Parameter[1].Value.Text[0].StartsWith("TA91"))
            {
                CommandText = "SELECT * FROM " + t.mod.Parameters[1].Parameter[1].Value.Text[0].Substring(0, 4) + $" WHERE ID = '{t.pr.ident_collect[(byte)SeqIdentDataInCollect.plantnum].value.Trim('\'', '\"')}'";
            }

            // Дискриптор БД
            SqliteConnection db = new SqliteConnection(str);

            // Объект, с помощью которого будут выполняться команды
            SqliteCommand command = new SqliteCommand(CommandText, db);
            db.Open();
            
            // Экземпляр, использующийся для извлечения данных связанных с БД
            SqliteDataReader rd = command.ExecuteReader();
            rd.Read();

            // Проверка наличия сохраненной конфигурации
            if (rd.HasRows)
                for (int k = 1; k <= t.pr.conf_param.Count; k++)
                {
                    if ((t.pr.conf_param[k - 1].children != null) && (t.pr.conf_param[k - 1].children.Count > 0))
                        WriteCollect(t.pr.conf_param[k - 1].children, Helper.ReformatArray(rd.GetString(k)));
                    else
                        t.pr.conf_param[k - 1].value = rd.GetString(k);
                }
            db.Close();
        }

        public int WriteCollect(ObservableCollection<TypeParam> children, byte[] value, int cnt = 0)
        {
            byte size;
            byte[] arr;
            
            foreach (TypeParam p in children)
            {
                if ((p.children != null) && (p.children.Count > 0))
                    cnt += WriteCollect(p.children, value, cnt);
                else
                {
                    size = Helper.SizeOfType(p.data_type);
                    arr = new byte[size];
                    Array.Copy(value, cnt, arr, 0, size);
                    cnt += size;
                    p.value = Helper.GetString(p.data_type, arr);
                }
            }
            return cnt;
        }

        public int WorkSaveBD(Module m)
        {
            CommandText = null;
            SqliteConnection db = new SqliteConnection(str);

            byte[] tempr = null;
            string[] arr_string = new string[3]; ;

            if ((m.pr.ident_collect[(byte)SeqIdentDataInCollect.plantnum].value.Trim('\'', '\"') != null) && 
                (m.pr.ident_collect[(byte)SeqIdentDataInCollect.plantnum].value.Trim('\'', '\"') != string.Empty))
            { 
                if (m.mod.Parameters[1].Parameter[1].Value.Text[0].StartsWith("TD9"))
                {
                    CommandText += "REPLACE INTO  " + m.mod.Parameters[1].Parameter[1].Value.Text[0][0] + m.mod.Parameters[1].Parameter[1].Value.Text[0][1] + m.mod.Parameters[1].Parameter[1].Value.Text[0][2] + "  (ID, TOReset, Mode, Rate";
                    for (int i = 1; i < m.pr.conf_param.Count() - 2; i++)
                        CommandText += $",  TimeSettings_{i}";
                    CommandText += $") VALUES ({m.pr.ident_collect[(byte)SeqIdentDataInCollect.plantnum].value.Trim('\'', '\"')}, {m.pr.conf_param[0].value.Trim('\'', '\"')}," +
                        $" {m.pr.conf_param[1].value.Trim('\'', '\"')}, {m.pr.conf_param[2].value.Trim('\'', '\"')}";

                    for (int i = 3; i < m.pr.conf_param.Count(); i++)
                    {
                        tempr = m.pr.conf_param[i].GetBytes();
                        for (int j = 0; j < 3; j++)
                        {
                            if (tempr[j].ToString("X").Length > 1)
                                arr_string[j] = tempr[j].ToString("X");
                            else
                                arr_string[j] = "0" + tempr[j].ToString("X");

                        }
                        CommandText += $", '{arr_string[0]}{arr_string[1]}{arr_string[2]}'";
                    }
                    CommandText += ")";
                }
                else if (m.mod.Parameters[1].Parameter[1].Value.Text[0].StartsWith("TA91"))
                {
                    CommandText += "REPLACE INTO  " + m.mod.Parameters[1].Parameter[1].Value.Text[0] + "  (ID, TOReset, Mode, Rate";
                    for (int i = 1; i < m.pr.conf_param.Count() - 2; i++)
                    {
                        CommandText += $",  Channel{i}";
                    }
                    CommandText += $") VALUES ('{m.pr.ident_collect[(byte)SeqIdentDataInCollect.plantnum].value.Trim('\'', '\"')}', '{m.pr.conf_param[0].value.Trim('\'', '\"')}'," +
                        $" '{m.pr.conf_param[1].value.Trim('\'', '\"')}', '{m.pr.conf_param[2].value.Trim('\'', '\"')}'";

                    for (int i = 3; i < m.pr.conf_param.Count(); i++)
                    {
                        CommandText += $",  '{m.pr.conf_param[i].GetBytes()}'";
                    }
                    CommandText += ")";
                }
                else if (m.mod.Parameters[1].Parameter[1].Value.Text[0].StartsWith("TA901"))
                {
                    CommandText += "REPLACE INTO  " + m.mod.Parameters[1].Parameter[1].Value.Text[0] + "  (ID, TOReset, Mode, Rate";
                    for (int i = 1; i < m.pr.conf_param.Count() - 2; i++)
                    {
                        CommandText += $",  Channel{i}";
                    }
                    CommandText += $") VALUES ('{m.pr.ident_collect[(byte)SeqIdentDataInCollect.plantnum].value.Trim('\'', '\"')}', '{m.pr.conf_param[0].value.Trim('\'', '\"')}'," +
                        $" '{m.pr.conf_param[1].value.Trim('\'', '\"')}', '{m.pr.conf_param[2].value.Trim('\'', '\"')}'";

                    for (int i = 3; i < m.pr.conf_param.Count(); i++)
                    {
                        CommandText += $",  '{m.pr.conf_param[i].GetBytes()}'";
                    }
                    CommandText += ")";
                }
                else if (m.mod.Parameters[1].Parameter[1].Value.Text[0].StartsWith("TA902"))
                {
                    CommandText += "REPLACE INTO  " + m.mod.Parameters[1].Parameter[1].Value.Text[0] + "  (ID, TOReset, Mode, PerSend, AdcRate";
                    for (int i = 1; i < m.pr.conf_param.Count() - 3; i++)
                    {
                        CommandText += $",  SigType_{i}";
                    }
                    CommandText += $") VALUES ('{m.pr.ident_collect[(byte)SeqIdentDataInCollect.plantnum].value.Trim('\'', '\"')}', '{m.pr.conf_param[0].value.Trim('\'', '\"')}'," +
                        $" '{m.pr.conf_param[1].value.Trim('\'', '\"')}', '{m.pr.conf_param[2].value.Trim('\'', '\"')}'," +
                        $" '{m.pr.conf_param[3].value.Trim('\'', '\"')}' ";

                    for (int i = 4; i < m.pr.conf_param.Count(); i++)
                    {
                        CommandText += $",  '{m.pr.conf_param[i].GetBytes()}'";
                    }
                    CommandText += ")";
                }

                SqliteCommand command = new SqliteCommand(CommandText, db);
                db.Open();
                try { command.ExecuteNonQuery(); }
                catch
                {  
                    db.Close();
                    return 2;
                }
            }
            else
            {
                db.Close();
                return 1;
            }  
            db.Close();
            return 0;
        }
    }
}
