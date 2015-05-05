using System;
using System.Xml;

using System.Reflection;
using Microsoft.CSharp;
using Microsoft.VisualBasic;
using System.CodeDom;
using System.CodeDom.Compiler;

using DigitalPlatform.Xml;

namespace DigitalPlatform.rms.Client
{
	/// <summary>
	/// C#�ű�����ʵ��ģ��
    /// TODO: ���Կ��Ƿ�ֹ����ScriptManager����
	/// </summary>
	public class Script
	{
		// ��references.xml�ļ��еõ�refs�ַ�������
		// return:
		//		-1	error
		//		0	��ȷ
		public static int GetRefs(string strRef,
			out string [] saRef,
			out string strError)
		{
			saRef = null;
			strError = "";
			XmlDocument dom = new XmlDocument();

			try 
			{
				dom.LoadXml(strRef);
			}
			catch (Exception ex)
			{
				strError = ex.Message;
				return -1;
			}

			// ����ref�ڵ�
			XmlNodeList nodes = dom.DocumentElement.SelectNodes("//ref");
			saRef = new string [nodes.Count];
			for(int i=0;i<nodes.Count;i++)
			{
				saRef[i] = DomUtil.GetNodeText(nodes[i]);
			}

			return 0;
		}

		// ����Assembly
		// parameters:
		//	strCode:	�ű�����
		//	refs:	���ӵ��ⲿassembly
		// strResult:������Ϣ
		// objDb:���ݿ�����ڳ����getErrorInfo�õ�
		// ����ֵ:�����õ�Assembly
		public static Assembly CreateAssembly(string strCode,
			string[] refs,
			string strLibPaths,
			string strOutputFile,
			out string strErrorInfo,
			out string strWarningInfo)
		{
			// System.Reflection.Assembly compiledAssembly = null;
			strErrorInfo = "";
			strWarningInfo = "";
 
			// CompilerParameters����
			System.CodeDom.Compiler.CompilerParameters compilerParams;
			compilerParams = new CompilerParameters();

			compilerParams.GenerateInMemory = true; //Assembly is created in memory
			// compilerParams.IncludeDebugInformation = true;

			if (strOutputFile != null && strOutputFile != "") 
			{
				compilerParams.GenerateExecutable = false;
				compilerParams.OutputAssembly = strOutputFile;
				// compilerParams.CompilerOptions = "/t:library";
			}

			if (strLibPaths != null && strLibPaths != "")	// bug
				compilerParams.CompilerOptions = "/lib:" + strLibPaths;

			compilerParams.TreatWarningsAsErrors = false;
			compilerParams.WarningLevel = 4;
 
			// ���滯·����ȥ������ĺ��ַ���
			// RemoveRefsBinDirMacro(ref refs);

			compilerParams.ReferencedAssemblies.AddRange(refs);


			CSharpCodeProvider provider;

			// System.CodeDom.Compiler.ICodeCompiler compiler;
			System.CodeDom.Compiler.CompilerResults results = null;
			try 
			{
				provider = new CSharpCodeProvider();
				// compiler = provider.CreateCompiler();
                results = provider.CompileAssemblyFromSource(
					compilerParams, 
					strCode);
			}
			catch (Exception ex) 
			{
				strErrorInfo = "���� " + ex.Message;
				return null;
			}

			int nErrorCount = 0;

			if (results.Errors.Count != 0) 
			{
				string strErrorString = "";
				nErrorCount = getErrorInfo(results.Errors,
					out strErrorString);

				strErrorInfo = "��Ϣ����:" + Convert.ToString(results.Errors.Count) + "\r\n";
				strErrorInfo += strErrorString;

				if (nErrorCount == 0 && results.Errors.Count != 0) 
				{
					strWarningInfo = strErrorInfo;
					strErrorInfo = "";
				}
			}

			if (nErrorCount != 0)
				return null;

 
			return results.CompiledAssembly;
		}

		// parameters:
		//		refs	���ӵ�refs�ļ�·����·���п��ܰ�����%installdir%
		public static int CreateAssemblyFile(string strCode,
			string[] refs,
			string strLibPaths,
			string strOutputFile,
			out string strErrorInfo,
			out string strWarningInfo)
		{
			// System.Reflection.Assembly compiledAssembly = null;
			strErrorInfo = "";
			strWarningInfo = "";
 
			// CompilerParameters����
			System.CodeDom.Compiler.CompilerParameters compilerParams;
			compilerParams = new CompilerParameters();

			compilerParams.GenerateInMemory = true; //Assembly is created in memory
			compilerParams.IncludeDebugInformation = true;

			if (strOutputFile != null && strOutputFile != "") 
			{
				compilerParams.GenerateExecutable = false;
				compilerParams.OutputAssembly = strOutputFile;
				// compilerParams.CompilerOptions = "/t:library";
			}

			if (strLibPaths != null && strLibPaths != "")	// bug
				compilerParams.CompilerOptions = "/lib:" + strLibPaths;

			compilerParams.TreatWarningsAsErrors = false;
			compilerParams.WarningLevel = 4;
 
			// ���滯·����ȥ������ĺ��ַ���
			// RemoveRefsBinDirMacro(ref refs);

			compilerParams.ReferencedAssemblies.AddRange(refs);


			CSharpCodeProvider provider;

			// System.CodeDom.Compiler.ICodeCompiler compiler;
			System.CodeDom.Compiler.CompilerResults results = null;
			try 
			{
				provider = new CSharpCodeProvider();
				// compiler = provider.CreateCompiler();
                results = provider.CompileAssemblyFromSource(
					compilerParams, 
					strCode);
			}
			catch (Exception ex) 
			{
				strErrorInfo = "���� " + ex.Message;
				return -1;
			}

			int nErrorCount = 0;

			if (results.Errors.Count != 0) 
			{
				string strErrorString = "";
				nErrorCount = getErrorInfo(results.Errors,
					out strErrorString);

				strErrorInfo = "��Ϣ����:" + Convert.ToString(results.Errors.Count) + "\r\n";
				strErrorInfo += strErrorString;

				if (nErrorCount == 0 && results.Errors.Count != 0) 
				{
					strWarningInfo = strErrorInfo;
					strErrorInfo = "";
				}
			}

			if (nErrorCount != 0)
				return -1;

 
			return 0;
		}

		// ���������Ϣ�ַ���
		public static int getErrorInfo(CompilerErrorCollection errors,
			out string strResult)
		{
			strResult = "";
			int nCount = 0;

			if (errors == null)
			{
				strResult = "error����Ϊnull";
				return 0;
			}
   
 
			foreach(CompilerError oneError in errors)
			{
				strResult += "(" + Convert.ToString(oneError.Line) + "," + Convert.ToString(oneError.Column) + ") ";
				strResult += (oneError.IsWarning) ? "warning " : "error ";
				strResult += oneError.ErrorNumber + " ";
				strResult += ": " + oneError.ErrorText + "\r\n";

				if (oneError.IsWarning == false)
					nCount ++;

			}
			return nCount;
		}

		public static Type GetDerivedClassType(Assembly assembly,
			string strBaseTypeFullName)
		{
			Type[] types = assembly.GetTypes();
			// string strText = "";

			for(int i=0;i<types.Length;i++) 
			{
				if (types[i].IsClass == false)
					continue;
				if (IsDeriverdFrom(types[i],
					strBaseTypeFullName) == true)
					return types[i];
			}


			return null;
		}


		// �۲�type�Ļ������Ƿ�������ΪstrBaseTypeFullName���ࡣ
		public static bool IsDeriverdFrom(Type type,
			string strBaseTypeFullName)
		{
			Type curType = type;
			for(;;) 
			{
				if (curType == null 
					|| curType.FullName == "System.Object")
					return false;

				if (curType.FullName == strBaseTypeFullName)
					return true;

				curType = curType.BaseType;
			}

		}

	}
}
