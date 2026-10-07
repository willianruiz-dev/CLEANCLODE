using ApiService.Models;
using AutoMapper;
using LocalDataBase;

namespace Domain
{
    public class ObjMapper
    {
        // Patron de Diseño Singleton
        private static IMapper? _instance;

        private ObjMapper() { }

        public static IMapper Instance
        {
            get
            {
                if (_instance == null)
                {
                    var mapperConfig = new MapperConfiguration(mc => mc.AddProfile(new MappingProfile()));
                    _instance = mapperConfig.CreateMapper();
                }
                return _instance;
            }
        }

    }

    internal class MappingProfile : Profile
    {
        public MappingProfile() 
        {
            CreateMap<DB_Transaction, TransactionDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.IdApi));

            CreateMap<TransactionDto, DB_Transaction>()
                .ForMember(dest => dest.IdApi, opt => opt.MapFrom(src => src.Id.ToString()))
                .ForMember(dest => dest.TypeTransaction, opt => opt.MapFrom(src => src.TypeTransaction ?? ""))
                .ForMember(dest => dest.TypePayment, opt => opt.MapFrom(src => src.TypePayment ?? ""))
                .ForMember(dest => dest.PayPad, opt => opt.MapFrom(src => src.PayPad ?? ""));

            CreateMap<DB_TransactionDetail, TransactionDetailDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.IdApi));
            CreateMap<TransactionDetailDto, DB_TransactionDetail>()
                .ForMember(dest => dest.IdApi, opt => opt.MapFrom(src => src.Id.ToString()))
                .ForMember(dest => dest.TypeOperation, opt => opt.MapFrom(src => src.TypeOperation ?? ""));
        }
    }
}
