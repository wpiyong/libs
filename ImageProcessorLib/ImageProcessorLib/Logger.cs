using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace ImageProcessorLib
{
    public static class Log
    {
        static Logger _logger = null;
        
        static Log()
        {
            var config = new NLog.Config.LoggingConfiguration();

            var fileTarget = new NLog.Targets.FileTarget("logfile")
            {
                FileName = "ImageProcessorLog.txt",
                Layout = "${longdate} ${level} ${message}  ${exception}",
                MaxArchiveFiles = 5,
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Rolling,
                ArchiveAboveSize = 100
            };

            config.AddTarget(fileTarget);

            LogManager.Configuration = config;

            _logger = LogManager.GetCurrentClassLogger();

        }

        public static void Trace(Exception ex)
        {
            _logger.Trace(ex, ex.Message);
        }

        public static void Debug(Exception ex)
        {
            _logger.Debug(ex, ex.Message);
        }

        public static void Error(Exception ex)
        {
            _logger.Error(ex, ex.Message);
        }
    }
}
