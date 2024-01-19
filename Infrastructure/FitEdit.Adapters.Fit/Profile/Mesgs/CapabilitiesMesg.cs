#region Copyright
/////////////////////////////////////////////////////////////////////////////////////////////
// Copyright 2023 Garmin International, Inc.
// Licensed under the Flexible and Interoperable Data Transfer (FIT) Protocol License; you
// may not use this file except in compliance with the Flexible and Interoperable Data
// Transfer (FIT) Protocol License.
/////////////////////////////////////////////////////////////////////////////////////////////
// ****WARNING****  This file is auto-generated!  Do NOT edit this file.
// Profile Version = 21.105Release
// Tag = production/release/21.105.00-0-gdc65d24
/////////////////////////////////////////////////////////////////////////////////////////////

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Linq;

namespace Dynastream.Fit
{
    /// <summary>
    /// Implements the Capabilities profile message.
    /// </summary>
    public class CapabilitiesMesg : Mesg
    {
        #region Fields
        #endregion

        /// <summary>
        /// Field Numbers for <see cref="CapabilitiesMesg"/>
        /// </summary>
        public sealed class FieldDefNum
        {
            public const byte Languages = 0;
            public const byte Sports = 1;
            public const byte WorkoutsSupported = 21;
            public const byte ConnectivitySupported = 23;
            public const byte Invalid = Fit.FieldNumInvalid;
        }

        #region Constructors
        public CapabilitiesMesg() : base(Profile.GetMesg(MesgNum.Capabilities))
        {
        }

        public CapabilitiesMesg(Mesg mesg) : base(mesg)
        {
        }
        #endregion // Constructors

        #region Methods
        
        /// <summary>
        ///
        /// </summary>
        /// <returns>returns number of elements in field Languages</returns>
        public int GetNumLanguages()
        {
            return GetNumFieldValues(0, Fit.SubfieldIndexMainField);
        }

        ///<summary>
        /// Retrieves the Languages field
        /// Comment: Use language_bits_x types where x is index of array.</summary>
        /// <param name="index">0 based index of Languages element to retrieve</param>
        /// <returns>Returns nullable byte representing the Languages field</returns>
        public byte? GetLanguages(int index)
        {
            Object val = GetFieldValue(0, index, Fit.SubfieldIndexMainField);
            if(val == null)
            {
                return null;
            }

            return (Convert.ToByte(val));
            
        }

        /// <summary>
        /// Set Languages field
        /// Comment: Use language_bits_x types where x is index of array.</summary>
        /// <param name="index">0 based index of languages</param>
        /// <param name="languages_">Nullable field value to be set</param>
        public void SetLanguages(int index, byte? languages_)
        {
            SetFieldValue(0, index, languages_, Fit.SubfieldIndexMainField);
        }
        
        
        /// <summary>
        ///
        /// </summary>
        /// <returns>returns number of elements in field Sports</returns>
        public int GetNumSports()
        {
            return GetNumFieldValues(1, Fit.SubfieldIndexMainField);
        }

        ///<summary>
        /// Retrieves the Sports field
        /// Comment: Use sport_bits_x types where x is index of array.</summary>
        /// <param name="index">0 based index of Sports element to retrieve</param>
        /// <returns>Returns nullable byte representing the Sports field</returns>
        public byte? GetSports(int index)
        {
            Object val = GetFieldValue(1, index, Fit.SubfieldIndexMainField);
            if(val == null)
            {
                return null;
            }

            return (Convert.ToByte(val));
            
        }

        /// <summary>
        /// Set Sports field
        /// Comment: Use sport_bits_x types where x is index of array.</summary>
        /// <param name="index">0 based index of sports</param>
        /// <param name="sports_">Nullable field value to be set</param>
        public void SetSports(int index, byte? sports_)
        {
            SetFieldValue(1, index, sports_, Fit.SubfieldIndexMainField);
        }
        
        ///<summary>
        /// Retrieves the WorkoutsSupported field</summary>
        /// <returns>Returns nullable uint representing the WorkoutsSupported field</returns>
        public uint? GetWorkoutsSupported()
        {
            Object val = GetFieldValue(21, 0, Fit.SubfieldIndexMainField);
            if(val == null)
            {
                return null;
            }

            return (Convert.ToUInt32(val));
            
        }

        /// <summary>
        /// Set WorkoutsSupported field</summary>
        /// <param name="workoutsSupported_">Nullable field value to be set</param>
        public void SetWorkoutsSupported(uint? workoutsSupported_)
        {
            SetFieldValue(21, 0, workoutsSupported_, Fit.SubfieldIndexMainField);
        }
        
        ///<summary>
        /// Retrieves the ConnectivitySupported field</summary>
        /// <returns>Returns nullable uint representing the ConnectivitySupported field</returns>
        public uint? GetConnectivitySupported()
        {
            Object val = GetFieldValue(23, 0, Fit.SubfieldIndexMainField);
            if(val == null)
            {
                return null;
            }

            return (Convert.ToUInt32(val));
            
        }

        /// <summary>
        /// Set ConnectivitySupported field</summary>
        /// <param name="connectivitySupported_">Nullable field value to be set</param>
        public void SetConnectivitySupported(uint? connectivitySupported_)
        {
            SetFieldValue(23, 0, connectivitySupported_, Fit.SubfieldIndexMainField);
        }
        
        #endregion // Methods
    } // Class
} // namespace