#region Copyright 
// Copyright 2017 Gygya Inc.  All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License.  
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDER AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.
#endregion

using System;
using Hyperscale.Microcore.Interfaces.SystemWrappers;

namespace Hyperscale.Microcore.ServiceDiscovery
{
    public class DeploymentIdentifier
    {
        /// <summary>The name of the service (e.g. "AccountsService").</summary>
        public string ServiceName { get; }

        /// <summary>
        /// Create a new identifier for a service which is deployed on a different datacenter
        /// </summary>
        public DeploymentIdentifier(string serviceName)
        {
            if (serviceName == null)
                throw new ArgumentNullException();
            ServiceName = serviceName;
        }

        public override string ToString()
        {
            var serviceAndEnv = ServiceName;

            return $"{serviceAndEnv}";
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj is DeploymentIdentifier other)
            {
                return ServiceName == other.ServiceName;
            }
            else
                return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ServiceName.GetHashCode();
                return hashCode;
            }
        }

    }
}