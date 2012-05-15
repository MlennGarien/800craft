using System;
using System.IO;
using fCraft;
using LuaInterface;

namespace fCraft
{
    public class LuaPlugin
    {
        private Lua _lua = new Lua();
        private string _initFile;

        internal LuaPlugin(string initfile){
            _initFile = initfile;
        }

        internal void Start(){
            _lua.RegisterFunction("Is", this, this.GetType().GetMethod("Is"));
            if (!File.Exists(_initFile)){
                Logger.Log(LogType.Error, "Initfile '" + _initFile + "' not found.");
                return;
            } try { 
                _lua.DoFile(_initFile); 
            }
            catch (Exception e) { 
                LogError(e);
            }
        }

        internal void Stop(){
            _lua.Close();
        }

        internal void LogError(Exception e){
            string problem = "Lua Error: " + e;
            if (e is LuaScriptException){
                LuaScriptException ex = (LuaScriptException)e;
                if (ex.IsNetException) { e = e.InnerException; }
                problem = "Lua Error in " + ex.Source + e;
            } 
            Logger.Log(LogType.Error, problem);
        }

        public bool Is(object value, string type){
            if (value == null) { return false; }
            Type t = value.GetType();
            do if (t.ToString().Equals(type, StringComparison.OrdinalIgnoreCase)) { return true; }
            while ((t = t.BaseType) != null);
            return false;
        }
    }
}
