﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SD.LLBLGen.Pro.ORMSupportClasses;
using ServiceStack.Text;
using Northwind.Data;
using Northwind.Data.Dtos;
using Northwind.Data.EntityClasses;
using Northwind.Data.FactoryClasses;
using Northwind.Data.HelperClasses;
using Northwind.Data.ServiceInterfaces;
using Northwind.Data.Services;

namespace Northwind.Data.ServiceRepositories
{ 
    public partial class CustomerCustomerDemoServiceRepository : EntityServiceRepositoryBase<CustomerCustomerDemo, CustomerCustomerDemoEntity, CustomerCustomerDemoEntityFactory, CustomerCustomerDemoFieldIndex>, ICustomerCustomerDemoServiceRepository
    {
        public override IDataAccessAdapterFactory DataAccessAdapterFactory { get; set; }
        protected override EntityType EntityType
        {
            get { return EntityType.CustomerCustomerDemoEntity; }
        }

        // Description for parameters: http://datatables.net/usage/server-side
        public DataTableResponse GetDataTableResponse(CustomerCustomerDemoDataTableRequest request)
        {
            //UrlDecode Request Properties
            request.sSearch = System.Web.HttpUtility.UrlDecode(request.sSearch);
            request.Sort = System.Web.HttpUtility.UrlDecode(request.Sort);
            request.Include = System.Web.HttpUtility.UrlDecode(request.Include);
            request.Filter = System.Web.HttpUtility.UrlDecode(request.Filter);
            request.Relations = System.Web.HttpUtility.UrlDecode(request.Relations);
            request.Select = System.Web.HttpUtility.UrlDecode(request.Select);

            //Paging
            var iDisplayStart = request.iDisplayStart + 1; // this is because it passes in the 0 instead of 1, 10 instead of 11, etc...
            iDisplayStart = iDisplayStart <= 0 ? (1+((request.PageNumber-1)*request.PageSize)): iDisplayStart;
            var iDisplayLength = request.iDisplayLength <= 0 ? request.PageSize: request.iDisplayLength;
            var pageNumber = Math.Ceiling(iDisplayStart*1.0/iDisplayLength);
            var pageSize = iDisplayLength;
            //Sorting
            var sort = request.Sort;
            if (request.iSortingCols > 0 && request.iSortCol_0 >= 0)
            {
                sort = string.Format("{0}:{1}", FieldMap.Keys.ElementAt(Convert.ToInt32(request.iSortCol_0)), request.sSortDir_0);
            }
            //Search
            var filter = request.Filter;
            var searchStr = string.Empty;
            if (!string.IsNullOrEmpty(request.sSearch))
            {
                // process int field searches
                var n = 0;
                var searchStrAsInt = -1;
                if (int.TryParse(request.sSearch, out searchStrAsInt))
                {
                    foreach (var fm in FieldMap)
                    {
                        if (fm.Value.DataType.IsNumericType())
                        {
                            n++;
                            searchStr += string.Format("({0}:eq:{1})", fm.Key, searchStrAsInt);
                        }
                    }
                }
                // process string field searches
                foreach (var fm in FieldMap)
                {
                    if (fm.Value.DataType == typeof (string)/* && fm.Value.MaxLength < 2000*/)
                    {
                        n++;
                        searchStr += string.Format("({0}:lk:*{1}*)", fm.Key, request.sSearch);
                    }
                }
                searchStr = n > 1 ? "(|" + searchStr + ")": searchStr.Trim('(', ')');

                filter = string.IsNullOrEmpty(filter) ? searchStr
                    : string.Format("(^{0}{1})", 
                    filter.StartsWith("(") ? filter: "(" + filter + ")",
                    searchStr.StartsWith("(") ? searchStr : "(" + searchStr + ")");
            }

            var entities = Fetch(new 
CustomerCustomerDemoQueryCollectionRequest
                {
                    Filter = filter, 
                    PageNumber = Convert.ToInt32(pageNumber),
                    PageSize = pageSize,
                    Sort = sort,
                    Include = request.Include,
                    Relations = request.Relations,
                    Select = request.Select,
                });
            var response = new DataTableResponse();
            var includeCustomer = ((request.Include ?? "").IndexOf("customer", StringComparison.InvariantCultureIgnoreCase)) >= 0;
            var includeCustomerDemographic = ((request.Include ?? "").IndexOf("customerdemographic", StringComparison.InvariantCultureIgnoreCase)) >= 0;

            foreach (var item in entities.Result)
            {
                var relatedDivs = new List<string>();
                relatedDivs.Add(string.Format(@"<div style=""display:block;""><span class=""badge badge-info"">{0}</span><a href=""/customers?filter=customerid:eq:{2}"">{1} Customer</a></div>",
                            includeCustomer ? (item.Customer==null?"0":"1"): "",
                            includeCustomer ? "": "",
                            item.CustomerId
                        ));
                relatedDivs.Add(string.Format(@"<div style=""display:block;""><span class=""badge badge-info"">{0}</span><a href=""/customerdemographics?filter=customertypeid:eq:{2}"">{1} Customer Demographic</a></div>",
                            includeCustomerDemographic ? (item.CustomerDemographic==null?"0":"1"): "",
                            includeCustomerDemographic ? "": "",
                            item.CustomerTypeId
                        ));

                response.aaData.Add(new string[]
                    {
                        item.CustomerId,
                        item.CustomerTypeId,

                        string.Join("", relatedDivs.ToArray())
                    });
            }
            response.sEcho = request.sEcho;
            // total records in the database before datatables search
            response.iTotalRecords = entities.Paging.TotalCount;
            // total records in the database after datatables search
            response.iTotalDisplayRecords = entities.Paging.TotalCount;
            return response;
        }

        public CustomerCustomerDemoCollectionResponse Fetch(CustomerCustomerDemoQueryCollectionRequest request)
        {
            var totalItemCount = 0;
            var entities = base.Fetch(request.Sort, request.Include, request.Filter,
                                      request.Relations, request.Select, request.PageNumber,
                                      request.PageSize, out totalItemCount);
            var response = new CustomerCustomerDemoCollectionResponse(entities.ToDtoCollection(), request.PageNumber,
                                                          request.PageSize, totalItemCount);
            return response;
        }
    

        public CustomerCustomerDemoResponse Fetch(CustomerCustomerDemoPkRequest request)
        {
            var entity = new CustomerCustomerDemoEntity();
            entity.CustomerId = request.CustomerId;
            entity.CustomerTypeId = request.CustomerTypeId;

            var prefetchPath = ConvertStringToPrefetchPath(EntityType, request.Include);
            var excludedIncludedFields = ConvertStringToExcludedIncludedFields(request.Select);

            using (var adapter = DataAccessAdapterFactory.NewDataAccessAdapter())
            {
                if (adapter.FetchEntity(entity, prefetchPath, null, excludedIncludedFields))
                {
                    return new CustomerCustomerDemoResponse(entity.ToDto());
                }
            }

            throw new NullReferenceException();
        }

        public CustomerCustomerDemoResponse Create(CustomerCustomerDemoAddRequest request)
        {
            var entity = request.FromDto();
            entity.IsNew = true;

            using (var adapter = DataAccessAdapterFactory.NewDataAccessAdapter())
            {
                if(adapter.SaveEntity(entity, true))
                {
                    return new CustomerCustomerDemoResponse(entity.ToDto());
                }
            }

            throw new InvalidOperationException();
        }

        public CustomerCustomerDemoResponse Update(CustomerCustomerDemoUpdateRequest request)
        {
            var entity = request.FromDto();
            entity.IsNew = false;
            entity.IsDirty = true;

            using (var adapter = DataAccessAdapterFactory.NewDataAccessAdapter())
            {
                if (adapter.SaveEntity(entity, true))
                {
                    return new CustomerCustomerDemoResponse(entity.ToDto());
                }
            }

            throw new InvalidOperationException();
        }

        public bool Delete(CustomerCustomerDemoDeleteRequest request)
        {
            var entity = new CustomerCustomerDemoEntity();
            entity.CustomerId = request.CustomerId;
            entity.CustomerTypeId = request.CustomerTypeId;


            using (var adapter = DataAccessAdapterFactory.NewDataAccessAdapter())
            {
                return adapter.DeleteEntity(entity);
            }
        }

        private const string UcMapCacheKey = "customercustomerdemo-uc-map";
        internal override IDictionary< string, IEntityField2[] > UniqueConstraintMap
        {
            get 
            { 
                var map = CacheClient.Get<IDictionary< string, IEntityField2[] >>(UcMapCacheKey);
                if (map == null)
                {
                    map = new Dictionary< string, IEntityField2[] >();
                    CacheClient.Set(UcMapCacheKey, map);
                }
                return map;
            }
            set { }
        }
    }

    internal static class CustomerCustomerDemoEntityDtoMapperExtensions
    {
        public static CustomerCustomerDemo ToDto(this CustomerCustomerDemoEntity entity)
        {
            return entity.ToDto(new Hashtable(), new Hashtable());
        }

        public static CustomerCustomerDemo ToDto(this CustomerCustomerDemoEntity entity, Hashtable seenObjects, Hashtable parents)
        {
            var dto = new CustomerCustomerDemo();
            if (entity != null)
            {
                if (seenObjects == null)
                    seenObjects = new Hashtable();
                seenObjects[entity] = dto;

                parents = new Hashtable(parents) { { entity, null } };

              // Map dto properties
                dto.CustomerId = entity.CustomerId;
                dto.CustomerTypeId = entity.CustomerTypeId;


              // Map dto associations
              // n:1 Customer association
              if (entity.Customer != null)
              {
                dto.Customer = entity.Customer.RelatedObject(seenObjects, parents);
              }
              // n:1 CustomerDemographic association
              if (entity.CustomerDemographic != null)
              {
                dto.CustomerDemographic = entity.CustomerDemographic.RelatedObject(seenObjects, parents);
              }

            }
            return dto;
        }

        public static CustomerCustomerDemoCollection ToDtoCollection(this EntityCollection<CustomerCustomerDemoEntity> entities)
        {
            var seenObjects = new Hashtable();
            var collection = new CustomerCustomerDemoCollection();
            foreach (var entity in entities)
            {
                collection.Add(entity.ToDto(seenObjects, new Hashtable()));
            }
            return collection;
        }

        public static CustomerCustomerDemoEntity FromDto(this CustomerCustomerDemo dto)
        {
            var entity = new CustomerCustomerDemoEntity();

            // Map entity properties
            entity.CustomerId = dto.CustomerId;
            entity.CustomerTypeId = dto.CustomerTypeId;


            // Map entity associations
            // n:1 Customer association
            if (dto.Customer != null)
            {
        entity.Customer = dto.Customer.FromDto();
            }
            // n:1 CustomerDemographic association
            if (dto.CustomerDemographic != null)
            {
        entity.CustomerDemographic = dto.CustomerDemographic.FromDto();
            }

            return entity;
        }

        public static CustomerCustomerDemo[] RelatedArray(this EntityCollection<CustomerCustomerDemoEntity> entities, Hashtable seenObjects, Hashtable parents)
        {
            if (null == entities)
            {
                return null;
            }

            var arr = new CustomerCustomerDemo[entities.Count];
            var i = 0;

            foreach (var entity in entities)
            {
                if (parents.Contains(entity))
                {
                    // - avoid all cyclic references and return null
                    // - another option is to 'continue' and just disregard this one entity; however,
                    // if this is a collection this would lead the client app to believe that other
                    // items are part of the collection and not the parent item, which is misleading and false
                    // - it is therefore better to just return null, indicating nothing is being retrieved
                    // for the property all-together
                    return null;
                }
            }

            foreach (var entity in entities)
            {
                if (seenObjects.Contains(entity))
                {
                    arr[i++] = seenObjects[entity] as CustomerCustomerDemo;
                }
                else
                {
                    arr[i++] = entity.ToDto(seenObjects, parents);
                }
            }
            return arr;
        }

        public static CustomerCustomerDemo RelatedObject(this CustomerCustomerDemoEntity entity, Hashtable seenObjects, Hashtable parents)
        {
            if (null == entity)
            {
                return null;
            }

            if (seenObjects.Contains(entity))
            {
                if (parents.Contains(entity))
                {
                    // avoid cyclic references
                    return null;
                }
                else
                {
                    return seenObjects[entity] as CustomerCustomerDemo;
                }
            }

            return entity.ToDto(seenObjects, parents);
        }
    }
}