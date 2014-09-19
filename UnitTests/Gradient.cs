/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using System;
using Gabardine.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gabardine.UnitTests
{
    [TestClass]
    public unsafe class Gradient
    {
        [TestMethod]
        public void Rosenbrock()
        {
            Gabardine.UserSettings.LoadSettings(@"..\..\..\user_settings.json");
            Context ctx = new Context();
            ScriptParser parser = new ScriptParser(ctx);
            var files = new string[] {
                @"..\..\..\scripts\type.cnut",
                @"..\..\..\scripts\boolean.cnut",
                @"..\..\..\scripts\real.cnut",
                @"..\..\..\scripts\nat.cnut",
                @"..\..\..\scripts\list.cnut",
                @"..\..\..\scripts\matrix.cnut",
                @"..\..\..\scripts\pair.cnut",
                @"..\..\..\scripts\summation.cnut",
                @"..\..\..\scripts\diff.cnut",
                @"..\..\..\scripts\mkl.cnut",
                @"..\..\..\scripts\iterate.cnut",
                @"..\..\..\scripts\array.cnut",
            };

            const string test_script = @"
    require ..\..\..\scripts\summation.cnut
    param u typeof(u) -> Real
    param v typeof(v) -> Real
	let obj = (1. - u)^2. + 100.*(v - u^2.)^2.
    
    let theta = pair(u,v)
    print rewrite pair_to_vector(diff(obj, theta))
      
/*
    operator gd 3
    gd(x, f, init) ->
	    let g = diff(f, x)
	    let cnd = x => dot(g,g) < 1e-3
	    let upd = x => madd(x, scale(1e-3, g))
	    iterate(cnd, upd, init) 

    size(gd(_,_,init)) -> size(init)

    param t0
    inherit(t0) -> theta

    print rewrite gd(theta, obj, t0)    
/*
module Gradient {
    function rosenbrock {
		
		in double* theta0
		inherit(theta0) -> theta

		out double* gd(theta, obj, theta0)
		//out double* newton(theta0, theta, obj, 0.5)
	}
}
*/
";

            parser.ParseFiles(files);
            bool success = parser.Parse(test_script);
            Assert.IsTrue(success, "Parser failed.");

        }
    }
}
