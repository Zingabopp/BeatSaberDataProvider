using SongFeedReaders;
using System;
using System.Collections.Generic;
using System.Text;

namespace SongFeedReadersTests
{
    public static class TestSetup
    {
        public static bool IsInitialized { get; private set; }
        public static void Initialize()
        {
            if (IsInitialized)
                return;
            IsInitialized = true;
            if (!WebUtils.IsInitialized)
            {
                WebUtils.Initialize(new WebUtilities.WebWrapper.WebClientWrapper());
                // WebUtils.Initialize(new WebUtilities.HttpClientWrapper.HttpClientWrapper());
            }

        }
    }
}
