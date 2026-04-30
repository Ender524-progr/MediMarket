using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace MediMarket.web.Models
{
    public partial class ConexionModel : DbContext
    {
        public ConexionModel()
            : base("name=ConexionModel")
        {
        }

        public virtual DbSet<administradores> administradores { get; set; }
        public virtual DbSet<categorias> categorias { get; set; }
        public virtual DbSet<clinicas> clinicas { get; set; }
        public virtual DbSet<detalle_pedidos> detalle_pedidos { get; set; }
        public virtual DbSet<lista_deseos> lista_deseos { get; set; }
        public virtual DbSet<pedidos> pedidos { get; set; }
        public virtual DbSet<planes> planes { get; set; }
        public virtual DbSet<producto_imagenes> producto_imagenes { get; set; }
        public virtual DbSet<productos> productos { get; set; }
        public virtual DbSet<proveedores> proveedores { get; set; }
        public virtual DbSet<suscripciones> suscripciones { get; set; }
        public virtual DbSet<usuarios> usuarios { get; set; }
        public virtual DbSet<solicitudes_rfq> solicitudes_rfq { get; set; }
        public virtual DbSet<cotizaciones> cotizaciones { get; set; }
        public virtual DbSet<producto_comentarios> producto_comentarios { get; set; }


        public virtual DbSet<notificaciones_proveedores> notificaciones_proveedores { get; set; }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<categorias>()
                .HasMany(e => e.categorias1)
                .WithOptional(e => e.categorias2)
                .HasForeignKey(e => e.padre_id);

            modelBuilder.Entity<categorias>()
                .HasMany(e => e.productos)
                .WithOptional(e => e.categorias)
                .HasForeignKey(e => e.categoria_id);

            modelBuilder.Entity<clinicas>()
                .HasMany(e => e.lista_deseos)
                .WithRequired(e => e.clinicas)
                .HasForeignKey(e => e.clinica_id);

            modelBuilder.Entity<clinicas>()
                .HasMany(e => e.pedidos)
                .WithRequired(e => e.clinicas)
                .HasForeignKey(e => e.clinica_id)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<detalle_pedidos>()
                .Property(e => e.precio_unitario)
                .HasPrecision(12, 2);

            modelBuilder.Entity<detalle_pedidos>()
                .Property(e => e.subtotal)
                .HasPrecision(23, 2);

            modelBuilder.Entity<pedidos>()
                .Property(e => e.total)
                .HasPrecision(12, 2);

            modelBuilder.Entity<pedidos>()
                .HasMany(e => e.detalle_pedidos)
                .WithRequired(e => e.pedidos)
                .HasForeignKey(e => e.pedido_id);

            modelBuilder.Entity<planes>()
                .Property(e => e.precio_mensual)
                .HasPrecision(12, 2);

            modelBuilder.Entity<planes>()
                .HasMany(e => e.suscripciones)
                .WithRequired(e => e.planes)
                .HasForeignKey(e => e.plan_id)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<productos>()
                .Property(e => e.precio_unitario)
                .HasPrecision(12, 2);

            modelBuilder.Entity<productos>()
                .HasMany(e => e.detalle_pedidos)
                .WithRequired(e => e.productos)
                .HasForeignKey(e => e.producto_id)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<productos>()
                .HasMany(e => e.lista_deseos)
                .WithRequired(e => e.productos)
                .HasForeignKey(e => e.producto_id)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<productos>()
                .HasMany(e => e.producto_imagenes)
                .WithRequired(e => e.productos)
                .HasForeignKey(e => e.producto_id);

            modelBuilder.Entity<proveedores>()
                .HasMany(e => e.productos)
                .WithRequired(e => e.proveedores)
                .HasForeignKey(e => e.proveedor_id);

            modelBuilder.Entity<usuarios>()
                .HasMany(e => e.clinicas)
                .WithRequired(e => e.usuarios)
                .HasForeignKey(e => e.usuario_id);

            modelBuilder.Entity<usuarios>()
                .HasMany(e => e.proveedores)
                .WithRequired(e => e.usuarios)
                .HasForeignKey(e => e.usuario_id);

            modelBuilder.Entity<usuarios>()
                .HasMany(e => e.suscripciones)
                .WithRequired(e => e.usuarios)
                .HasForeignKey(e => e.usuario_id);

            modelBuilder.Entity<solicitudes_rfq>()
                .HasRequired(e => e.clinicas)
                .WithMany()
                .HasForeignKey(e => e.clinica_id);

            modelBuilder.Entity<solicitudes_rfq>()
                .HasOptional(e => e.categorias)
                .WithMany()
                .HasForeignKey(e => e.categoria_id);

            // ─── CONFIGURACIÓN DE NOTIFICACIONES PROVEEDORES ───
            modelBuilder.Entity<proveedores>()
                .HasMany(e => e.notificaciones_proveedores)
                .WithRequired(e => e.proveedores)
                .HasForeignKey(e => e.proveedor_id)
                .WillCascadeOnDelete(false); // Evita errores en SQL Server

            // Le decimos que estos campos usan NVARCHAR (Unicode)
            modelBuilder.Entity<notificaciones_proveedores>()
                .Property(e => e.titulo)
                .IsUnicode(true);

            modelBuilder.Entity<notificaciones_proveedores>()
                .Property(e => e.mensaje)
                .IsUnicode(true);
        }
    }
}
