using System;
using System.Data;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Maticsoft.Utility;
using Maticsoft.IDBO;
using Maticsoft.CodeHelper;
namespace Maticsoft.DbObjects.SQLite
{
	/// <summary>
	/// ���ݿ�ű������ࡣScript
	/// </summary>
	public class DbScriptBuilder:IDbScriptBuilder
	{
        #region ˽�б���
        protected string _key = "ID";//��ʶ�У��������ֶ�		
        protected string _keyType = "int";//��ʶ�У��������ֶ�����        
        #endregion

		#region ����
		private string _dbconnectStr;
		private string _dbname;
		private string _tablename;		
        private string _procprefix;
        private string _projectname;
        private List<ColumnInfo> _fieldlist;
        private List<ColumnInfo> _keys; //�����������ֶ��б� 
        

		public string DbConnectStr
		{
			set{_dbconnectStr=value;}
			get{return _dbconnectStr;}
		}
		public string DbName
		{
			set{ _dbname=value;}
			get{return _dbname;}
		}
		public string TableName
		{
			set{ _tablename=value;}
			get{return _tablename;}
		}
        
        /// <summary>
        /// �洢����ǰ׺ 
        /// </summary>       
        public string ProcPrefix
        {
            set { _procprefix = value; }
            get { return _procprefix; }
        }
        /// <summary>
        /// ��Ŀ���� 
        /// </summary>        
        public string ProjectName
        {
            set { _projectname = value; }
            get { return _projectname; }
        }

        public List<ColumnInfo> Keys
        {
            set { _keys = value; }
            get { return _keys; }
        }
        /// <summary>
        /// ѡ����ֶμ���
        /// </summary>
        public List<ColumnInfo> Fieldlist
        {
            set { _fieldlist = value; }
            get { return _fieldlist; }
        }         
		#endregion


        #region ��������
        /// <summary>
        /// ѡ����ֶμ��ϵ�-�ַ���
        /// </summary>
        public string Fields
        {
            get
            {
                StringPlus _fields = new StringPlus();
                foreach (ColumnInfo obj in Fieldlist)
                {
                    _fields.Append("'" + obj.ColumnName + "',");
                }
                _fields.DelLastComma();
                return _fields.Value;
            }
        }
        /// <summary>
        /// �ֶε� select * �б�
        /// </summary>
        public string Fieldstrlist
        {
            get
            {
                StringPlus _fields = new StringPlus();
                foreach (ColumnInfo obj in Fieldlist)
                {
                    _fields.Append(obj.ColumnName + ",");
                }
                _fields.DelLastComma();
                return _fields.Value;
            }
        }
        /// <summary>
        /// �����Ƿ��б�ʶ��
        /// </summary>
        public bool IsHasIdentity
        {
            get
            {
                bool isid = false;
                if (_keys.Count > 0)
                {
                    foreach (ColumnInfo key in _keys)
                    {
                        if (key.IsIdentity)
                        {
                            isid = true;
                        }
                    }
                }
                return isid;
            }
        }
        #endregion

        DbObject dbobj=new DbObject();
		public DbScriptBuilder()
		{
        }

        #region ��������

        /// <summary>
        /// �õ���������������б� (���磺����Exists  Delete  GetModel �Ĳ�������)
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public string GetInParameter(List<ColumnInfo> fieldlist, bool output)
        {
            StringPlus strclass = new StringPlus();

            foreach (ColumnInfo field in fieldlist)
            {
                string columnName = field.ColumnName;
                string columnType = field.TypeName;
                bool IsIdentity = field.IsIdentity;
                bool IsPK = field.IsPrimaryKey;
                string Length = field.Length;
                string Preci = field.Precision;
                string Scale = field.Scale;               
                switch (columnType.ToLower())
                {
                    case "decimal":
                    case "numeric":
                        strclass.Append("@" + columnName + " " + columnType + "(" + Preci + "," + Scale + ")");
                        break;                    
                    case "char":
                    case "varchar":
                    case "varbinary":
                    case "binary":
                    case "nchar":
                    case "nvarchar":    
                        {
                            strclass.Append("@" + columnName + " " + columnType + "(" + CodeCommon.GetDataTypeLenVal(columnType.ToLower(), Length) + ")");
                        }
                        break;                       
                    default:
                        strclass.Append("@" + columnName + " " + columnType);
                        break;
                }
                if ((IsIdentity) && (output))
                {
                    strclass.AppendLine(" output,");                    
                    continue;
                }
                strclass.AppendLine(",");
            }
            strclass.DelLastComma();
            return strclass.Value;
        }

        /// <summary>
        /// �õ�Where������� - Parameter��ʽ (���磺����Exists  Delete  GetModel ��where)
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public string GetWhereExpression(List<ColumnInfo> keys)
        {
            StringPlus strclass = new StringPlus();
            foreach (ColumnInfo key in keys)
            {
                strclass.Append(key.ColumnName + "=@" + key.ColumnName + " and ");
            }
            strclass.DelLastChar("and");
            return strclass.Value;
        }

        
        #endregion

        #region �������ݿ�������ű�

        /// <summary>
        /// �������ݿ����б��Ĵ����ű�
        /// </summary>
        /// <returns></returns>
        public string CreateDBTabScript(string dbname)
        {
            dbobj.DbConnectStr = this.DbConnectStr;
            StringPlus strclass = new StringPlus();
            List<string> tabNames = dbobj.GetTables(dbname);
            if (tabNames.Count > 0)
            {
                foreach (string tabname in tabNames)
                {
                    strclass.AppendLine(CreateTabScript(dbname, tabname));
                }
            }
            return strclass.Value;
        }

        /// <summary>
		/// �������ݿ�������ű�
		/// </summary>
		/// <returns></returns>
		public string CreateTabScript(string dbname,string tablename)
		{
			dbobj.DbConnectStr=_dbconnectStr;
			List<ColumnInfo> collist =dbobj.GetColumnInfoList(dbname,tablename);
            DataTable dt = Maticsoft.CodeHelper.CodeCommon.GetColumnInfoDt(collist);
            StringPlus strclass=new StringPlus();
 		            
			StringPlus ColdefaVal=new StringPlus();//�ֶε�Ĭ��ֵ�б�			
			
			Hashtable FildtabList=new Hashtable();//�ֶ��б�
			StringPlus FildList=new StringPlus();//�ֶ��б�
            
            
            //��ʼ������
            strclass.AppendLine("");
            strclass.Append(dbobj.GetObjectInfo(dbname, tablename));
            strclass.AppendLine(";");

            if (dt != null)
            {
                DataRow[] dtrows;
                if (Fieldlist.Count > 0)
                {
                    dtrows = dt.Select("ColumnName in (" + Fields + ")", "colorder asc");
                }
                else
                {
                    dtrows = dt.Select();
                }
                foreach (DataRow row in dtrows)
                {
                    string columnName = row["ColumnName"].ToString();
                    string columnType = row["TypeName"].ToString();
                    string IsIdentity = row["IsIdentity"].ToString();
                    if (IsIdentity == "��")
                    {
                        continue;
                    }
                    FildtabList.Add(columnName, columnType);
                    FildList.Append("" + columnName + ",");                                      
                }
            }           
            FildList.DelLastComma();
			
			//��ȡ����
			DataTable dtdata=dbobj.GetTabData(dbname,tablename);
			if(dtdata!=null)
			{				
				foreach(DataRow row in dtdata.Rows)//ѭ��������
				{	
					StringPlus strfild=new StringPlus();
					StringPlus strdata=new StringPlus();
					string [] split= FildList.Value.Split(new Char [] { ','});

					foreach(string fild in split)//ѭ��һ�����ݵĸ����ֶ�
					{
                        string colname = fild;
						string coltype="";
						foreach (DictionaryEntry myDE in FildtabList)
						{
							if(myDE.Key.ToString()==colname)
							{
								coltype=myDE.Value.ToString();
							}
						}	
						string strval="";
						switch(coltype)
						{
							case "binary":
							{
								byte[] bys=(byte[])row[colname];
								strval=Maticsoft.CodeHelper.CodeCommon.ToHexString(bys);
							}
								break;
							case "bit":
							{
								strval=(row[colname].ToString().ToLower()=="true")?"1":"0";
							}
								break;
							default:
								strval=row[colname].ToString().Trim();
								break;
						}
						if(strval!="")
						{
                            if (Maticsoft.CodeHelper.CodeCommon.IsAddMark(coltype))
							{
                                //strdata.Append("'"+strval+"',");
                                strdata.Append("'" + strval.Replace("'", "''") + "',");
							}
							else
							{
								strdata.Append(strval+",");
							}	
							strfild.Append("["+colname+"],");
						}

					}					
					strfild.DelLastComma();
					strdata.DelLastComma();
					//��������INSERT���
					strclass.Append("INSERT ["+tablename+"] (");
					strclass.Append(strfild.Value);
					strclass.Append(") VALUES ( ");
					strclass.Append(strdata.Value);//����ֵ
					strclass.AppendLine(");");
				}
			}
			return strclass.Value;
		}

        /// <summary>
        /// ����SQL��ѯ��� �������ݴ����ű�
        /// </summary>
        /// <returns></returns>
        public string CreateTabScriptBySQL(string dbname,string strSQL)
        {
            dbobj.DbConnectStr = _dbconnectStr;
            //string PKfild = "";//�����ֶ�
            bool IsIden = false;//�Ƿ��Ǳ�ʶ�ֶ�
            string tablename = "TableName";
            StringPlus strclass = new StringPlus();

            #region ��ѯ����
            int ns = strSQL.IndexOf(" from ");
            if (ns > 0)
            {
                string sqltemp = strSQL.Substring(ns + 5).Trim();
                int ns2 = sqltemp.IndexOf(" ");
                if (sqltemp.Length > 0)
                {
                    if (ns2 > 0)
                    {
                        tablename = sqltemp.Substring(0, ns2).Trim();
                    }
                    else
                    {
                        tablename = sqltemp.Substring(0).Trim();
                    }
                }
            }
            tablename = tablename.Replace("[", "").Replace("]", "");

            #endregion

            #region ���ݽű�


            //��ȡ����
            DataTable dtdata = dbobj.GetTabDataBySQL(dbname, strSQL);
            if (dtdata != null)
            {
                DataColumnCollection dtcols = dtdata.Columns;
                foreach (DataRow row in dtdata.Rows)//ѭ��������
                {
                    StringPlus strfild = new StringPlus();
                    StringPlus strdata = new StringPlus();

                    foreach (DataColumn col in dtcols)//ѭ��һ�����ݵĸ����ֶ�
                    {
                        string colname = col.ColumnName;
                        string coltype = col.DataType.Name;
                        if (col.AutoIncrement)
                        {
                            IsIden = true;
                        }
                        string strval = "";
                        switch (coltype.ToLower())
                        {
                            case "binary":
                            case "byte[]":
                            case "blob":
                                {
                                    byte[] bys = (byte[])row[colname];
                                    strval = Maticsoft.CodeHelper.CodeCommon.ToHexString(bys);
                                }
                                break;
                            case "bit":
                            case "boolean":
                                {
                                    strval = (row[colname].ToString().ToLower() == "true") ? "1" : "0";
                                }
                                break;
                            default:
                                strval = row[colname].ToString().Trim();
                                break;
                        }
                        if (strval != "")
                        {
                            if (Maticsoft.CodeHelper.CodeCommon.IsAddMark(coltype))
                            {
                                //strdata.Append("'" + strval + "',");
                                strdata.Append("'" + strval.Replace("'", "''") + "',");
                            }
                            else
                            {
                                strdata.Append(strval + ",");
                            }
                            strfild.Append("[" + colname + "],");
                        }

                    }
                    strfild.DelLastComma();
                    strdata.DelLastComma();
                    //��������INSERT���
                    strclass.Append("INSERT [" + tablename + "] (");
                    strclass.Append(strfild.Value);
                    strclass.Append(") VALUES ( ");
                    strclass.Append(strdata.Value);//����ֵ
                    strclass.AppendLine(")");
                }
            }
            
            #endregion
            return strclass.Value;

        }
        	
		/// <summary>
		/// �������ݿ�������ű����ļ�
		/// </summary>
		/// <returns></returns>
		public void CreateTabScript(string dbname,string tablename,string filename,System.Windows.Forms.ProgressBar progressBar)
		{
			StreamWriter sw=new StreamWriter(filename,true,Encoding.Default);//,false);

			dbobj.DbConnectStr=_dbconnectStr;
            //DataTable dt=dbobj.GetColumnInfoList(dbname,tablename);
            List<ColumnInfo> collist =dbobj.GetColumnInfoList(dbname,tablename);
            DataTable dt = Maticsoft.CodeHelper.CodeCommon.GetColumnInfoDt(collist);

			StringPlus strclass=new StringPlus();
            //strclass.AppendLine("if exists (select * from sysobjects where id = OBJECT_ID('["+tablename+"]') and OBJECTPROPERTY(id, 'IsUserTable') = 1) ");
            //strclass.AppendLine("DROP TABLE '"+tablename+"'");
			
            //string PKfild="";//�����ֶ�
            //bool IsIden=false;//�Ƿ��Ǳ�ʶ�ֶ�
			StringPlus ColdefaVal=new StringPlus();//�ֶε�Ĭ��ֵ�б�
			
			
			Hashtable FildtabList=new Hashtable();//�ֶ��б�
			StringPlus FildList=new StringPlus();//�ֶ��б�

            strclass.AppendLine("");
            strclass.Append(dbobj.GetObjectInfo(dbname, tablename));
            strclass.AppendLine(";");

            if (dt != null)
            {
                DataRow[] dtrows;
                if (Fieldlist.Count > 0)
                {
                    dtrows = dt.Select("ColumnName in (" + Fields + ")", "colorder asc");
                }
                else
                {
                    dtrows = dt.Select();
                }
                foreach (DataRow row in dtrows)
                {
                    string columnName = row["ColumnName"].ToString();
                    string columnType = row["TypeName"].ToString();
                    string IsIdentity = row["IsIdentity"].ToString();
                    if (IsIdentity == "��")
                    {
                        continue;
                    }
                    FildtabList.Add(columnName, columnType);
                    FildList.Append("" + columnName + ",");
                }
            }
            FildList.DelLastComma();
								
			sw.Write(strclass.Value);

			#region �������ݽű�

			//��ȡ����
			DataTable dtdata=dbobj.GetTabData(dbname,tablename);
			if(dtdata!=null)
			{		
				int i=0;				
				progressBar.Maximum=dtdata.Rows.Count;				
				foreach(DataRow row in dtdata.Rows)//ѭ��������
				{
                    i++;
					progressBar.Value=i;
					
					StringPlus rowdata=new StringPlus();

					StringPlus strfild=new StringPlus();
					StringPlus strdata=new StringPlus();
					string [] split= FildList.Value.Split(new Char [] { ','});

					foreach(string fild in split)//ѭ��һ�����ݵĸ����ֶ�
					{
						string colname=fild;
						string coltype="";
						foreach (DictionaryEntry myDE in FildtabList)
						{
							if(myDE.Key.ToString()==colname)
							{
								coltype=myDE.Value.ToString();
							}
						}	
						string strval="";
						switch(coltype)
						{
							case "binary":
							{
								byte[] bys=(byte[])row[colname];
                                strval = Maticsoft.CodeHelper.CodeCommon.ToHexString(bys);
							}
								break;
							case "bit":
							{
								strval=(row[colname].ToString().ToLower()=="true")?"1":"0";
							}
								break;
							default:
								strval=row[colname].ToString().Trim();
								break;
						}
						if(strval!="")
						{
                            if (Maticsoft.CodeHelper.CodeCommon.IsAddMark(coltype))
							{
                                //strdata.Append("'"+strval+"',");
                                strdata.Append("'" + strval.Replace("'", "''") + "',");
							}
							else
							{
								strdata.Append(strval+",");
							}	
							strfild.Append("["+colname+"],");
						}

					}					
					strfild.DelLastComma();
					strdata.DelLastComma();

					//��������INSERT���
					rowdata.Append("INSERT ["+tablename+"] (");
					rowdata.Append(strfild.Value);
					rowdata.Append(") VALUES ( ");
					rowdata.Append(strdata.Value);//����ֵ
					rowdata.AppendLine(");");
					sw.Write(rowdata.Value);
				}
			}

			#endregion
            					
			
			sw.Flush();
			sw.Close();
            		
		}
        
		#endregion

		
        #region �����洢����
        /// <summary>
        /// �õ�һ���������б��Ĵ洢����
        /// </summary>
        /// <param name="DbName"></param>
        /// <returns></returns>
        public string GetPROCCode(string dbname)
        {
            return "";
        }
        /// <summary>
        /// �õ�ĳ�����Ĵ洢����
        /// </summary>
        /// <param name="dbname">����</param>
        /// <param name="tablename">����</param>
        /// <returns></returns>
        public string GetPROCCode(string dbname, string tablename)
        {
            return "";
        }

        /// <summary>
        /// �õ�ĳ�����Ĵ洢���̣�ѡ�����ɵķ�����
        /// </summary>
        public string GetPROCCode(bool Maxid, bool Ishas, bool Add, bool Update, bool Delete, bool GetModel, bool List)
        {
            return "";
        }        
        
        public string CreatPROCGetMaxID()
		{
            StringPlus strclass = new StringPlus();
            if (_keys.Count > 0)
            {
                string keyname = "";
                foreach (ColumnInfo obj in _keys)
                {
                    if (CodeCommon.DbTypeToCS(obj.TypeName) == "int")
                    {
                        keyname = obj.ColumnName;
                        if (obj.IsPrimaryKey)
                        {
                            //strclass.Append("if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[");
                            //strclass.Append("" + ProcPrefix + "" + _tablename + "_GetMaxId");
                            //strclass.AppendLine("]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)");
                            //strclass.AppendLine("drop procedure [dbo].[" + ProcPrefix + "" + _tablename + "_GetMaxId]");
                            //strclass.AppendLine("GO");
                            strclass.AppendLine("------------------------------------");
                            strclass.AppendLine("--��;���õ������ֶ����ֵ ");
                            strclass.AppendLine("--��Ŀ���ƣ�" + ProjectName);
                            strclass.AppendLine("--˵����");
                            strclass.AppendLine("--ʱ�䣺" + DateTime.Now.ToString());
                            strclass.AppendLine("------------------------------------");
                            strclass.AppendLine("CREATE PROCEDURE " + ProcPrefix + "" + _tablename + "_GetMaxId");
                            strclass.AppendLine("AS");
                            strclass.AppendSpaceLine(1, "DECLARE @TempID int");
                            strclass.AppendSpaceLine(1, "SELECT @TempID = max([" + keyname + "])+1 FROM " + _tablename);
                            strclass.AppendSpaceLine(1, "IF @TempID IS NULL");
                            strclass.AppendSpaceLine(2, "RETURN 1");
                            strclass.AppendSpaceLine(1, "ELSE");
                            strclass.AppendSpaceLine(2, "RETURN @TempID");
                            strclass.AppendLine("");
                            strclass.AppendLine("GO");		
                            break;
                        }
                    }
                }
            }
            return strclass.ToString();			
		}
        public string CreatPROCIsHas()
		{						
			StringPlus strclass=new StringPlus();			
			
            //strclass.Append("if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[");
            //strclass.Append("" + ProcPrefix + ""+_tablename+"_Exists");
            //strclass.AppendLine("]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)");
            //strclass.AppendLine("drop procedure [dbo].[" + ProcPrefix + ""+_tablename+"_Exists]");
            //strclass.AppendLine("GO");
            strclass.AppendLine("------------------------------------");
            strclass.AppendLine("--��;���Ƿ��Ѿ����� ");
            strclass.AppendLine("--��Ŀ���ƣ�" + ProjectName);
            strclass.AppendLine("--˵����");
            strclass.AppendLine("--ʱ�䣺" + DateTime.Now.ToString());
            strclass.AppendLine("------------------------------------");
			strclass.AppendLine("CREATE PROCEDURE " + ProcPrefix + ""+_tablename+"_Exists");

            strclass.AppendLine(GetInParameter(Keys, false));
                       
			strclass.AppendLine("AS");
			strclass.AppendSpaceLine(1,"DECLARE @TempID int");
            strclass.AppendSpaceLine(1, "SELECT @TempID = count(1) FROM " + _tablename + " WHERE " + GetWhereExpression(Keys));
			strclass.AppendSpaceLine(1,"IF @TempID = 0");
			strclass.AppendSpaceLine(2,"RETURN 0");		
			strclass.AppendSpaceLine(1,"ELSE");		
			strclass.AppendSpaceLine(2,"RETURN 1");		
			strclass.AppendLine("");	
			strclass.AppendLine("GO");		
			return strclass.Value;
		}
		public string CreatPROCADD()
		{						
			StringPlus strclass=new StringPlus();
			StringPlus strclass1=new StringPlus();
			StringPlus strclass2=new StringPlus();			
			
            //strclass.Append("if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[");
            //strclass.Append("" + ProcPrefix + ""+_tablename+"_ADD");
            //strclass.AppendLine("]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)");
            //strclass.AppendLine("drop procedure [dbo].[" + ProcPrefix + ""+_tablename+"_ADD]");
            //strclass.AppendLine("GO");
            strclass.AppendLine("------------------------------------");
            strclass.AppendLine("--��;������һ����¼ ");
            strclass.AppendLine("--��Ŀ���ƣ�" + ProjectName);
            strclass.AppendLine("--˵����");
            strclass.AppendLine("--ʱ�䣺" + DateTime.Now.ToString());
            strclass.AppendLine("------------------------------------");
			strclass.AppendLine("CREATE PROCEDURE " + ProcPrefix + ""+_tablename+"_ADD");

            strclass.AppendLine(GetInParameter(Fieldlist, true));

            foreach (ColumnInfo field in Fieldlist)
            {
                string columnName = field.ColumnName;
                string columnType = field.TypeName;
                bool IsIdentity = field.IsIdentity;
                bool IsPK = field.IsPrimaryKey;
                string Length = field.Length;
                string Preci = field.Precision;
                string Scale = field.Scale;                			
                				
                if (IsIdentity)
                {
                    _key = columnName;
                    _keyType = columnType;
                    continue;
                }
                strclass1.Append("" + columnName + ",");
                strclass2.Append("@" + columnName + ",");
			}			
			
			strclass1.DelLastComma();
			strclass2.DelLastComma();
			strclass.AppendLine("");
			strclass.AppendLine(" AS ");
			strclass.AppendSpaceLine(1,"INSERT INTO "+_tablename+"(");
			strclass.AppendSpaceLine(1,strclass1.Value);
			strclass.AppendSpaceLine(1,")VALUES(");
			strclass.AppendSpaceLine(1,strclass2.Value);		
			strclass.AppendSpaceLine(1,")");
            if (IsHasIdentity) 
			{
                strclass.AppendSpaceLine(1, "SET @" + _key + " = @@IDENTITY");		
			}
			strclass.AppendLine("");	
			strclass.AppendLine("GO");		
			return strclass.Value;
		}
		public string CreatPROCUpdate()
		{						
			StringPlus strclass=new StringPlus();
			StringPlus strclass1=new StringPlus();						
			
            //strclass.Append("if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[");
            //strclass.Append("" + ProcPrefix + ""+_tablename+"_Update");
            //strclass.AppendLine("]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)");
            //strclass.AppendLine("drop procedure [dbo].[" + ProcPrefix + ""+_tablename+"_Update]");
            //strclass.AppendLine("GO");
            strclass.AppendLine("------------------------------------");
            strclass.AppendLine("--��;���޸�һ����¼ ");
            strclass.AppendLine("--��Ŀ���ƣ�" + ProjectName);
            strclass.AppendLine("--˵����");
            strclass.AppendLine("--ʱ�䣺" + DateTime.Now.ToString());
            strclass.AppendLine("------------------------------------");
			strclass.AppendLine("CREATE PROCEDURE " + ProcPrefix + ""+_tablename+"_Update");

            foreach (ColumnInfo field in Fieldlist)
			{				
                string columnName = field.ColumnName;
                string columnType = field.TypeName;
                bool IsIdentity = field.IsIdentity;
                bool IsPK = field.IsPrimaryKey;
                string Length = field.Length;
                string Preci = field.Precision;
                string Scale = field.Scale;
                
				switch(columnType.ToLower())
				{
					case "decimal":
					case "numeric":
						strclass.AppendLine("@"+columnName+" "+columnType+"("+Preci+","+Scale+"),");
						break;
                    case "varchar":
                    case "char":
                    case "nchar":
                    case "binary":
                    case "nvarchar":
                    case "varbinary":
                        {
                            string len = CodeCommon.GetDataTypeLenVal(columnType.Trim(), Length);
                            strclass.AppendLine("@" + columnName + " " + columnType + "(" + len + "),");
                        }
                        break;
					default:
						strclass.AppendLine("@"+columnName+" "+columnType+",");
						break;
				}
                if ((IsIdentity) || (IsPK))
				{					
					continue;					
				}					
				strclass1.Append(""+columnName+" = @"+columnName+",");
			}			
			strclass.DelLastComma();
			strclass1.DelLastComma();
			strclass.AppendLine();
			strclass.AppendLine(" AS ");
			strclass.AppendSpaceLine(1,"UPDATE "+_tablename+" SET ");
			strclass.AppendSpaceLine(1,strclass1.Value);
            strclass.AppendSpaceLine(1, "WHERE " + GetWhereExpression(Keys));	
			strclass.AppendLine();	
			strclass.AppendLine("GO");		
			return strclass.Value;
		}
        public string CreatPROCDelete()
		{					
			StringPlus strclass=new StringPlus();
						
            //strclass.Append("if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[");
            //strclass.Append("" + ProcPrefix + ""+_tablename+"_Delete");
            //strclass.AppendLine("]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)");
            //strclass.AppendLine("drop procedure [dbo].[" + ProcPrefix + ""+_tablename+"_Delete]");
            //strclass.AppendLine("GO");
            strclass.AppendLine("------------------------------------");
            strclass.AppendLine("--��;��ɾ��һ����¼ ");
            strclass.AppendLine("--��Ŀ���ƣ�" + ProjectName);
            strclass.AppendLine("--˵����");
            strclass.AppendLine("--ʱ�䣺" + DateTime.Now.ToString());
            strclass.AppendLine("------------------------------------");
			strclass.AppendLine("CREATE PROCEDURE " + ProcPrefix + ""+_tablename+"_Delete");

            strclass.AppendLine(GetInParameter(Keys, false));

			strclass.AppendLine(" AS ");
			strclass.AppendSpaceLine(1,"DELETE "+_tablename);
            strclass.AppendSpaceLine(1, " WHERE " + GetWhereExpression(Keys));	
			strclass.AppendLine("");	
			strclass.AppendLine("GO");		
			return strclass.Value;
			
		}
        public string CreatPROCGetModel()
		{		
			StringPlus strclass=new StringPlus();			
            //strclass.Append("if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[");
            //strclass.Append("" + ProcPrefix + ""+_tablename+"_GetModel");
            //strclass.AppendLine("]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)");
            //strclass.AppendLine("drop procedure [dbo].[" + ProcPrefix + ""+_tablename+"_GetModel]");
            //strclass.AppendLine("GO");
            strclass.AppendLine("------------------------------------");
            strclass.AppendLine("--��;���õ�ʵ��������ϸ��Ϣ ");
            strclass.AppendLine("--��Ŀ���ƣ�" + ProjectName);
            strclass.AppendLine("--˵����");
            strclass.AppendLine("--ʱ�䣺" + DateTime.Now.ToString());
            strclass.AppendLine("------------------------------------");
            
			strclass.AppendLine("CREATE PROCEDURE " + ProcPrefix + ""+_tablename+"_GetModel");

            strclass.AppendLine(GetInParameter(Keys, false));
            
			strclass.AppendLine(" AS ");
            strclass.AppendSpaceLine(1, "SELECT ");
            strclass.AppendSpaceLine(1, Fieldstrlist);
            strclass.AppendSpaceLine(1, " FROM " + _tablename);
            strclass.AppendSpaceLine(1, " WHERE " + GetWhereExpression(Keys));	
			strclass.AppendLine("");	
			strclass.AppendLine("GO");		
			return strclass.Value;
			
		}
		public string CreatPROCGetList()
		{						
			StringPlus strclass=new StringPlus();
						
            //strclass.Append("if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[");
            //strclass.Append("" + ProcPrefix + ""+_tablename+"_GetList");
            //strclass.AppendLine("]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)");
            //strclass.AppendLine("drop procedure [dbo].[" + ProcPrefix + ""+_tablename+"_GetList]");
            //strclass.AppendLine("GO");
            strclass.AppendLine("------------------------------------");
            strclass.AppendLine("--��;����ѯ��¼��Ϣ ");
            strclass.AppendLine("--��Ŀ���ƣ�" + ProjectName);
            strclass.AppendLine("--˵����");
            strclass.AppendLine("--ʱ�䣺" + DateTime.Now.ToString());
            strclass.AppendLine("------------------------------------");                        
			strclass.AppendLine("CREATE PROCEDURE " + ProcPrefix + ""+_tablename+"_GetList");						
			strclass.AppendLine(" AS ");
			strclass.AppendSpaceLine(1,"SELECT ");
            strclass.AppendSpaceLine(1, Fieldstrlist);			
			strclass.AppendSpaceLine(1," FROM "+_tablename);			
			//strclass.AppendSpaceLine(1," WHERE ");
			strclass.AppendLine("");	
			strclass.AppendLine("GO");		
			return strclass.Value;
			
		}		
		
		#endregion


        #region ����SQL��ѯ���

        /// <summary>
        /// ����Select��ѯ���
        /// </summary>
        /// <param name="dbname">����</param>
        /// <param name="tablename">����</param>
        /// <returns></returns>
        public string GetSQLSelect(string dbname, string tablename)
        {
            dbobj.DbConnectStr = _dbconnectStr;
            List<ColumnInfo> collist = dbobj.GetColumnList(dbname, tablename);
            
            this.DbName = dbname;
            this.TableName = tablename;
            StringPlus strsql = new StringPlus();
            strsql.Append("select ");

            if ((collist!=null)&&(collist.Count>0))
            {
                foreach (ColumnInfo col in collist)
                {                    
                    strsql.Append("" + col.ColumnName + ",");
                }
                strsql.DelLastComma();
            }
            strsql.Append(" from " + tablename);
            return strsql.Value;
        }

        /// <summary>
        /// ����update��ѯ���
        /// </summary>
        /// <param name="dbname">����</param>
        /// <param name="tablename">����</param>
        /// <returns></returns>
        public string GetSQLUpdate(string dbname, string tablename)
        {
            dbobj.DbConnectStr = _dbconnectStr;
            List<ColumnInfo> collist = dbobj.GetColumnList(dbname, tablename);
            this.DbName = dbname;
            this.TableName = tablename;
            StringPlus strsql = new StringPlus();
            strsql.AppendLine("update " + tablename + " set ");
            if ((collist!=null)&&(collist.Count > 0))
            {
                foreach (ColumnInfo col in collist)
                {                    
                    strsql.AppendLine("" + col.ColumnName + " = <" + col.ColumnName + ">,");
                }
                strsql.DelLastComma();
            }
            strsql.Append(" where <��������>");
            return strsql.Value;
        }
        /// <summary>
        /// ����update��ѯ���
        /// </summary>
        /// <param name="dbname">����</param>
        /// <param name="tablename">����</param>
        /// <returns></returns>
        public string GetSQLDelete(string dbname, string tablename)
        {
            dbobj.DbConnectStr = _dbconnectStr;
            this.DbName = dbname;
            this.TableName = tablename;
            StringPlus strsql = new StringPlus();
            strsql.AppendLine("delete from " + tablename);
            strsql.Append(" where  <��������>");
            return strsql.Value;
        }
        /// <summary>
        /// ����INSERT��ѯ���
        /// </summary>
        /// <param name="dbname">����</param>
        /// <param name="tablename">����</param>
        /// <returns></returns>
        public string GetSQLInsert(string dbname, string tablename)
        {
            dbobj.DbConnectStr = _dbconnectStr;            
            List<ColumnInfo> collist = dbobj.GetColumnList(dbname, tablename);
            this.DbName = dbname;
            this.TableName = tablename;
            StringPlus strsql = new StringPlus();
            StringPlus strsql2 = new StringPlus();
            strsql.AppendLine("INSERT INTO " + tablename + " ( ");

            if ((collist!=null)&&(collist.Count > 0))
            {
                foreach (ColumnInfo col in collist)
                {                                      
                    strsql.AppendLine("" + col.ColumnName + " ,");
                    if (Maticsoft.CodeHelper.CodeCommon.IsAddMark(col.TypeName))
                    {
                        strsql2.Append("'" + col.ColumnName + "',");
                    }
                    else
                    {
                        strsql2.Append(col.ColumnName + ",");
                    }
                    
                }
                strsql.DelLastComma();
                strsql2.DelLastComma();
            }
            strsql.Append(") VALUES (" + strsql2.Value + ")");
            return strsql.Value;
        }
        #endregion
	}
}