using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LocalDataBase.Services;

namespace LocalDataBase.Services
{
    public static class DB_PersonalInfoService
    {
        private static readonly HospitalApiService _apiService = new HospitalApiService();

        public static async Task<List<DB_UserPersonalInfo>> GetByDocument(string document)
        {
            try
            {
                var userDto = await _apiService.GetUserAsync(document);
                if (userDto != null)
                {
                    var user = new DB_UserPersonalInfo
                    {
                        Document = userDto.Document,
                        DocumentType = userDto.DocumentType,
                        Name = userDto.Name,
                        LastName = userDto.LastName,
                        Mobile = userDto.Mobile,
                        Email = userDto.Email
                    };
                    return new List<DB_UserPersonalInfo> { user };
                }
                return new List<DB_UserPersonalInfo>();
            }
            catch (Exception)
            {
                return new List<DB_UserPersonalInfo>();
            }
        }

        public static async Task<bool> Create(DB_UserPersonalInfo user)
        {
            try
            {
                var userDto = new UserPersonalInfoDto
                {
                    Document = user.Document,
                    DocumentType = user.DocumentType,
                    Name = user.Name,
                    LastName = user.LastName,
                    Mobile = user.Mobile,
                    Email = user.Email
                };

                await _apiService.CreateOrUpdateUserAsync(userDto);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
