//
// Copyright (C) Valve Corporation. All rights reserved.
//

using System;

namespace Phonon
{
    public class BinauralRenderer
    {
        public void Create(Environment environment, RenderingSettings renderingSettings, GlobalContext globalContext)
        {
            var error = PhononCore.iplCreateBinauralRenderer(globalContext, renderingSettings, null, ref binauralRenderer);
            if (error != Error.None)
                throw new Exception("Unable to create binaural renderer [" + error.ToString() + "]");
        }

        public IntPtr GetBinauralRenderer()
        {
            return binauralRenderer;
        }

        public void Destroy()
        {
            if (binauralRenderer != IntPtr.Zero)
                PhononCore.iplDestroyBinauralRenderer(ref binauralRenderer);
        }

        IntPtr binauralRenderer = IntPtr.Zero;
    }
}