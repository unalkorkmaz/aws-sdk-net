/*
 * Copyright 2010-2014 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 * 
 *  http://aws.amazon.com/apache2.0
 * 
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

/*
 * Do not modify this file. This file is generated from the medialive-2017-10-14.normal.json service model.
 */
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Text;
using System.IO;

using Amazon.Runtime;
using Amazon.Runtime.Internal;

namespace Amazon.MediaLive.Model
{
    /// <summary>
    /// SCTE-35 Delivery Restrictions.
    /// </summary>
    public partial class Scte35DeliveryRestrictions
    {
        private Scte35ArchiveAllowedFlag _archiveAllowedFlag;
        private Scte35DeviceRestrictions _deviceRestrictions;
        private Scte35NoRegionalBlackoutFlag _noRegionalBlackoutFlag;
        private Scte35WebDeliveryAllowedFlag _webDeliveryAllowedFlag;

        /// <summary>
        /// Gets and sets the property ArchiveAllowedFlag. SCTE-35 segmentation_descriptor archive_allowed_flag.
        /// </summary>
        public Scte35ArchiveAllowedFlag ArchiveAllowedFlag
        {
            get { return this._archiveAllowedFlag; }
            set { this._archiveAllowedFlag = value; }
        }

        // Check to see if ArchiveAllowedFlag property is set
        internal bool IsSetArchiveAllowedFlag()
        {
            return this._archiveAllowedFlag != null;
        }

        /// <summary>
        /// Gets and sets the property DeviceRestrictions. SCTE-35 segmentation_descriptor web_delivery_allowed_flag.
        /// </summary>
        public Scte35DeviceRestrictions DeviceRestrictions
        {
            get { return this._deviceRestrictions; }
            set { this._deviceRestrictions = value; }
        }

        // Check to see if DeviceRestrictions property is set
        internal bool IsSetDeviceRestrictions()
        {
            return this._deviceRestrictions != null;
        }

        /// <summary>
        /// Gets and sets the property NoRegionalBlackoutFlag. SCTE-35 segmentation_descriptor
        /// no_regional_blackout_flag.
        /// </summary>
        public Scte35NoRegionalBlackoutFlag NoRegionalBlackoutFlag
        {
            get { return this._noRegionalBlackoutFlag; }
            set { this._noRegionalBlackoutFlag = value; }
        }

        // Check to see if NoRegionalBlackoutFlag property is set
        internal bool IsSetNoRegionalBlackoutFlag()
        {
            return this._noRegionalBlackoutFlag != null;
        }

        /// <summary>
        /// Gets and sets the property WebDeliveryAllowedFlag. SCTE-35 segmentation_descriptor
        /// web_delivery_allowed_flag.
        /// </summary>
        public Scte35WebDeliveryAllowedFlag WebDeliveryAllowedFlag
        {
            get { return this._webDeliveryAllowedFlag; }
            set { this._webDeliveryAllowedFlag = value; }
        }

        // Check to see if WebDeliveryAllowedFlag property is set
        internal bool IsSetWebDeliveryAllowedFlag()
        {
            return this._webDeliveryAllowedFlag != null;
        }

    }
}