/*
 *
 * (c) Copyright Ascensio System Limited 2010-2018
 *
 * This program is freeware. You can redistribute it and/or modify it under the terms of the GNU 
 * General Public License (GPL) version 3 as published by the Free Software Foundation (https://www.gnu.org/copyleft/gpl.html). 
 * In accordance with Section 7(a) of the GNU GPL its Section 15 shall be amended to the effect that 
 * Ascensio System SIA expressly excludes the warranty of non-infringement of any third-party rights.
 *
 * THIS PROGRAM IS DISTRIBUTED WITHOUT ANY WARRANTY; WITHOUT EVEN THE IMPLIED WARRANTY OF MERCHANTABILITY OR
 * FITNESS FOR A PARTICULAR PURPOSE. For more details, see GNU GPL at https://www.gnu.org/copyleft/gpl.html
 *
 * You can contact Ascensio System SIA by email at sales@onlyoffice.com
 *
 * The interactive user interfaces in modified source and object code versions of ONLYOFFICE must display 
 * Appropriate Legal Notices, as required under Section 5 of the GNU GPL version 3.
 *
 * Pursuant to Section 7 § 3(b) of the GNU GPL you must retain the original ONLYOFFICE logo which contains 
 * relevant author attributions when distributing the software. If the display of the logo in its graphic 
 * form is not reasonably feasible for technical reasons, you must include the words "Powered by ONLYOFFICE" 
 * in every copy of the program you distribute. 
 * Pursuant to Section 7 § 3(e) we decline to grant you any rights under trademark law for use of our trademarks.
 *
*/


using ASC.Common.Module;
using ASC.FullTextIndex.Service.Config;
using log4net;
using log4net.Config;
using System;
using System.Diagnostics;
using System.ServiceModel;

namespace ASC.FullTextIndex.Service
{
    class FullTextIndexLauncher : IServiceController
    {
        private TextIndexerService indexer;
        private ServiceHost searcher;


        public void Start()
        {
            XmlConfigurator.Configure();
            try
            {
                var successInit = TextIndexCfg.Init();
                if (successInit && CheckIndexer(TextIndexCfg.IndexerName))
                {
                    indexer = new TextIndexerService();
                    indexer.Start();
                }

                searcher = new ServiceHost(typeof(TextSearcherService));
                searcher.Open();
            }
            catch (Exception e)
            {
                LogManager.GetLogger("ASC").Error(e);
                Stop();
            }
        }

        public void Stop()
        {
            TextSearcher.Instance.Stop();
            if (indexer != null)
            {
                indexer.Stop();
                indexer = null;
            }
            if (searcher != null)
            {
                searcher.Close();
                searcher = null;
            }
        }

        public static bool CheckIndexer(string fileName)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    CreateNoWindow = false,
                    UseShellExecute = false,
                    FileName = fileName,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true
                };

                using (Process.Start(startInfo))
                {
                }

                return true;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
