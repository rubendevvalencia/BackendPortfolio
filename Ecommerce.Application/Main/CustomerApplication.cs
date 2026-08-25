using AutoMapper;
using Ecommerce.Application.Dto;
using Ecommerce.Application.Interface;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common;
using Microsoft.IdentityModel.Tokens.Experimental;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.Service
{
    public class CustomerApplication : ICustomerApplication
    {
        private readonly ICustomerRepository _customerUoW;
        private readonly IMapper _mapper;
        public async Task<Response<bool>> AddAsync(CustomerDto customerDto)
        {
            var response = new Response<bool>();
            try
            {
                var customer = _mapper.Map<Ecommerce.Domain.Entity.Customer>(customerDto);
                response.Data = await _customerUoW.AddAsync(customer);
                if (response.Data && customer.Id != 0) response.IsSuccess = true;
            }
            catch (Exception ex) { response.Message = ex.Message; }
            return response;
        }

        public async Task<Response<bool>> DeleteAsync(int id)
        {
            var response = new Response<bool>();
            try
            {
                response.Data = await _customerUoW.DeleteAsync(id);
                if (response.Data) response.IsSuccess = true;
                else
                {
                    response.IsSuccess = false;
                    response.Message = $"Don't delete with ID {id}.";
                }
            }
            catch (Exception ex) { response.Message = ex.Message; }
            return response;
        }

        public async Task<Response<IEnumerable<CustomerDto>>> GetAllAsync()
        {
            var response = new Response<IEnumerable<CustomerDto>>();
            try
            {
                var customers = await _customerUoW.GetAllAsync();
                response.Data = _mapper.Map<IEnumerable<CustomerDto>>(customers);
                if (response.Data != null) response.IsSuccess = true;
            }
            catch (Exception ex) { response.Message = ex.Message; }
            return response;
        }

        public async Task<Response<CustomerDto?>> GetByIdAsync(int id)
        {
            var response = new Response<CustomerDto?>();
            try
            {
                var customer = await _customerUoW.GetByIdAsync(id);
                response.Data = _mapper.Map<CustomerDto?>(customer);
                if (response.Data != null) response.IsSuccess = true;
                else
                {
                    response.IsSuccess = false;
                    response.Message = $"Don't find with ID {id}.";
                }
            }
            catch (Exception ex) { response.Message = ex.Message; }
            return response;
        }

        public async Task<Response<bool>> UpdateAsync(CustomerDto customerDto)
        {
            var response = new Response<bool>();
            try
            {
                var customer = _mapper.Map<Ecommerce.Domain.Entity.Customer>(customerDto);
                response.Data = await _customerUoW.UpdateAsync(customer);
                if (response.Data) response.IsSuccess = true;
                else
                {
                    response.IsSuccess = false;
                    response.Message = $"Don't update with ID {customerDto.Id}.";
                }
            }
            catch (Exception ex) { response.Message = ex.Message; }
            return response;
        }
    }
}
