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


using System;
using System.Collections.Generic;
using System.IO;
using ASC.Core;
using ASC.CRM.Core;
using ASC.CRM.Core.Dao;
using ASC.Mail.Aggregator.Common;
using ASC.Mail.Aggregator.Common.DataStorage;
using ASC.Web.CRM.Core;
using Autofac;

namespace ASC.Mail.Aggregator.Dal
{
    public class CrmHistoryDal
    {
        private readonly int _tenant;
        private readonly Guid _user;

        private int Tenant { get { return _tenant; } }
        private Guid User { get { return _user; } }
        private string HttpContextScheme { get; set; }

        public CrmHistoryDal(int tenant, string user, string httpContextScheme)
        {
            _tenant = tenant;
            _user = new Guid(user);
            HttpContextScheme = httpContextScheme;

            if (SecurityContext.IsAuthenticated) return;

            CoreContext.TenantManager.SetCurrentTenant(Tenant);
            SecurityContext.AuthenticateMe(User);
        }

        public void AddRelationshipEvents(MailMessage message)
        {
            using (var scope = DIHelper.Resolve())
            {
                var factory = scope.Resolve<DaoFactory>();
                foreach (var contactEntity in message.LinkedCrmEntityIds)
                {
                    switch (contactEntity.Type)
                    {
                        case CrmContactEntity.EntityTypes.Contact:
                            var crmContact = factory.ContactDao.GetByID(contactEntity.Id);
                            CRMSecurity.DemandAccessTo(crmContact);
                            break;
                        case CrmContactEntity.EntityTypes.Case:
                            var crmCase = factory.CasesDao.GetByID(contactEntity.Id);
                            CRMSecurity.DemandAccessTo(crmCase);
                            break;
                        case CrmContactEntity.EntityTypes.Opportunity:
                            var crmOpportunity = factory.DealDao.GetByID(contactEntity.Id);
                            CRMSecurity.DemandAccessTo(crmOpportunity);
                            break;
                    }

                    var fileIds = new List<int>();

                    var apiHelper = new ApiHelper(HttpContextScheme);

                    foreach (var attachment in message.Attachments.FindAll(attach => !attach.isEmbedded))
                    {
                        if (attachment.dataStream != null)
                        {
                            attachment.dataStream.Seek(0, SeekOrigin.Begin);

                            var uploadedFileId = apiHelper.UploadToCrm(attachment.dataStream, attachment.fileName,
                                attachment.contentType, contactEntity);

                            if (uploadedFileId > 0)
                            {
                                fileIds.Add(uploadedFileId);
                            }
                        }
                        else
                        {
                            using (var file = AttachmentManager.GetAttachmentStream(attachment))
                            {
                                var uploadedFileId = apiHelper.UploadToCrm(file.FileStream, file.FileName,
                                    attachment.contentType, contactEntity);
                                if (uploadedFileId > 0)
                                {
                                    fileIds.Add(uploadedFileId);
                                }
                            }
                        }
                    }

                    apiHelper.AddToCrmHistory(message, contactEntity, fileIds);
                }
            }
        }
    }
}
