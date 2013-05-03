//
// --------------------------------------------------------------------------
//  Gurux Ltd
// 
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License 
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2. 
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gurux.Net
{
	/// <summary>
	/// Describes available settings for the media.
	/// </summary>
    [Flags]
    public enum AvailableMediaSettings
    {
        /// <summary>
        /// All network properties are shown.
        /// </summary>
        All = -1,
        /// <summary>
        /// Is Server is shown.
        /// </summary>
        Server = 0x1,
        /// <summary>
        /// Network protocol is shown.
        /// </summary>
        Protocol = 0x2,
        /// <summary>
        /// Port number is shown.
        /// </summary>
        Port = 0x4,
        /// <summary>
        /// Host Name is shown.
        /// </summary>
        Host = 0x8,
		/// <summary>
		/// Use IP v6 is shown.
		/// </summary>
		UseIPv6 = 0x10		
    };
}
