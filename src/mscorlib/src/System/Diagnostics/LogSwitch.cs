// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Collections;
using System.Runtime.Versioning;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics
{
    internal class LogSwitch
    {
        // ! WARNING ! 
        // If any fields are added/deleted/modified, perform the 
        // same in the EE code (debugdebugger.cpp/debugdebugger.h)
        internal String strName;
        internal String strDescription;
        private LogSwitch ParentSwitch;
        internal volatile LoggingLevels iLevel;
        internal volatile LoggingLevels iOldLevel;

        // ! END WARNING !


        private LogSwitch()
        {
        }

        // Constructs a LogSwitch.  A LogSwitch is used to categorize log messages.
        // 
        // All switches (except for the global LogSwitch) have a parent LogSwitch.
        //
        public LogSwitch(String name, String description, LogSwitch parent)
        {
            if (name != null && name.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(Name), SR.Argument_StringZeroLength);

            if ((name != null) && (parent != null))
            {
                strName = name;
                strDescription = description;
                iLevel = LoggingLevels.ErrorLevel;
                iOldLevel = iLevel;
                ParentSwitch = parent;

                Log.m_Hashtable.Add(strName, this);

                // Call into the EE to let it know about the creation of
                // this switch
                Log.AddLogSwitch(this);
            }
            else
                throw new ArgumentNullException((name == null ? nameof(name) : nameof(parent)));
        }

        internal LogSwitch(String name, String description)
        {
            strName = name;
            strDescription = description;
            iLevel = LoggingLevels.ErrorLevel;
            iOldLevel = iLevel;
            ParentSwitch = null;

            Log.m_Hashtable.Add(strName, this);

            // Call into the EE to let it know about the creation of
            // this switch
            Log.AddLogSwitch(this);
        }


        // Get property returns the name of the switch
        public virtual String Name
        {
            get { return strName; }
        }


        // Property to Get/Set the level of log messages which are "on" for the switch.  
        // 
        public virtual LoggingLevels MinimumLevel
        {
            get { return iLevel; }
            set
            {
                iLevel = value;
                iOldLevel = value;
                String strParentName = ParentSwitch != null ? ParentSwitch.Name : "";
                if (Debugger.IsAttached)
                    Log.ModifyLogSwitch((int)iLevel, strName, strParentName);

                Log.InvokeLogSwitchLevelHandlers(this, iLevel);
            }
        }


        // Checks if the given level is "on" for this switch or one of its parents.
        //
        public virtual bool CheckLevel(LoggingLevels level)
        {
            if (iLevel > level)
            {
                // recurse through the list till parent is hit. 
                if (ParentSwitch == null)
                    return false;
                else
                    return ParentSwitch.CheckLevel(level);
            }
            else
                return true;
        }


        // Returns a switch with the particular name, if any.  Returns null if no
        // such switch exists.
        public static LogSwitch GetSwitch(String name)
        {
            return (LogSwitch)Log.m_Hashtable[name];
        }
    }
}
