using System.ComponentModel.DataAnnotations.Schema;

namespace Ticketing.Query.Domain.ValueObjects
{
    [ComplexType] //Para EF: sin Id, sus columnas van en la tabla del propietario
    public class Address
    {
        public string Street { get; private set; }
        public string City { get; private set; }
        public string Country { get; private set; }

        //Constructor sin parámetros solo para que EF pueda materializar el tipo
        private Address()
        {
            Street = string.Empty;
            City = string.Empty;
            Country = string.Empty;
        }

        public Address(string street, string city, string country)
        {
            bool streetIsEmpty = string.IsNullOrWhiteSpace(street);
            if (streetIsEmpty) throw new ArgumentException("La calle es obligatoria.", nameof(street));
            
            bool cityIsEmpty = string.IsNullOrWhiteSpace(city);
            if (cityIsEmpty) throw new ArgumentException("La ciudad es obligatoria.", nameof(city));
            
            bool countryIsEmpty = string.IsNullOrWhiteSpace(country);
            if (countryIsEmpty) throw new ArgumentException("El país es obligatorio.", nameof(country));
            
            Street = street.Trim();
            City = city.Trim();
            Country = country.Trim();
        }
    }
}
