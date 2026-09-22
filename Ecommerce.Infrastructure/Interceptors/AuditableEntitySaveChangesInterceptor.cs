using Ecommerce.Domain.Entities.Audit;
using Ecommerce.Domain.Interface.IAuthIdentification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ecommerce.Infrastructure.Interceptors
{
    public class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor
    {
        //Esto ayuda a interceptar los cambios en las entidades auditables antes de que se guarden en la base de datos.
        //Con esto se configura automáticamente la información de auditoría (como CreatedAt, CreatedBy, LastUpdatedAt, LastUpdatedBy) para las entidades que heredan de BaseAuditEntity.

        private readonly ICurrentUser? _currentUser;

        //Opcional a proposito: el DesignTimeFactory y los tests lo crean con new() sin usuario y deben seguir guardando "System".
        public AuditableEntitySaveChangesInterceptor(ICurrentUser? currentUser = null)
        {
            _currentUser = currentUser;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            UpdateEntities(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public void UpdateEntities(DbContext? context)
        {
            if(context == null) throw new ArgumentNullException("context");

            var userName = _currentUser?.GetUserName();
            var auditUser = userName ?? "System"; //Sin token o fuera de una peticion HTTP no hay usuario: se queda "System".

            foreach(var entry in context.ChangeTracker.Entries<BaseAuditEntity>()) //Recorremos todas las entidades que heredan de BaseAuditEntity que están siendo rastreadas por el contexto.
            {
                if (entry.Entity is BaseAuditEntity auditableEntity)
                {
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditableEntity.CreatedAt = DateTime.UtcNow;
                            auditableEntity.CreatedBy = auditUser;
                            auditableEntity.LastUpdatedAt = DateTime.UtcNow;
                            auditableEntity.LastUpdatedBy = auditUser;
                            break;
                        case EntityState.Modified:
                            auditableEntity.LastUpdatedAt = DateTime.UtcNow;
                            auditableEntity.LastUpdatedBy = auditUser;
                            break;
                    }
                }
            }
        }
    }
}
