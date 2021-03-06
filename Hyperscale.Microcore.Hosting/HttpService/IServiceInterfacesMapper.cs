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
using System.Collections.Generic;

namespace Hyperscale.Microcore.Hosting.HttpService
{
	/// <summary>
	/// Mapping between service interfaces to grain interfaces.
	/// </summary>
    public interface IServiceInterfaceMapper
    {
		/// <summary>
		/// The service interface discovered in this application.
		/// </summary>
		IEnumerable<Type> ServiceInterfaceTypes { get; }

        /// <summary>
        /// The service implementing IHealthStatus
        /// </summary>
	    Type HealthStatusServiceType { get; set; }


	    /// <summary>
		/// A method that maps a service interface to its corresponding grain interface.
		/// </summary>
		/// <param name="serviceInterface">The service interface to map.</param>
		/// <returns>The grain interface for the provided service interface, or the provideded parameter (identity) when not used in Orelans.</returns>
		Type GetGrainInterface(Type serviceInterface);
    }

}
