# MiniShop

Boutique en ligne de démonstration (produits Apple, prix en dirhams) réalisée en ASP.NET Core MVC
avec Entity Framework Core et SQL Server LocalDB. Projet de TP : aucune vente réelle.

## Prérequis

- SDK .NET 10
- Visual Studio 2026, ou Visual Studio 2022 version 17.14 ou plus récente, avec la charge de travail
  « Développement web et ASP.NET » (« ASP.NET and web development »)
- SQL Server LocalDB (installé avec cette charge de travail)

Le projet cible `net10.0`.

## Ouvrir et lancer

1. Ouvrir `MiniShop.sln` (ou `MiniShop.slnx` avec Visual Studio 2026).
2. Appuyer sur F5.

Au premier lancement, la base `MiniShopDb` est créée dans LocalDB et remplie avec le catalogue
(20 produits, 3 catégories) : les migrations s'appliquent automatiquement en environnement
Development, celui des profils de lancement.

- Boutique : `/`
- Administration : `/admin` (pas encore de connexion)

## Remettre la base à zéro

Supprimer la base, puis relancer l'application (F5) : elle est recréée et remplie.

- Console du Gestionnaire de package : `Drop-Database`
- ou en ligne de commande, dans le dossier `MiniShop` : `dotnet ef database drop --force`
  (outil installé avec `dotnet tool install --global dotnet-ef`)
- ou dans l'Explorateur d'objets SQL Server : `(localdb)\MSSQLLocalDB` > Bases de données >
  `MiniShopDb` > Supprimer

## Structure

```
MiniShop/Controllers         boutique, panier, paiement (simulé), pages d'erreur
MiniShop/Areas/Admin         administration : produits, couleurs, stockage, catégories
MiniShop/Models, ViewModels  entités EF Core et modèles des vues
MiniShop/Data, Migrations    contexte EF Core, catalogue initial, migrations
MiniShop/Services            panier en session, images envoyées, catégories, formats
MiniShop/Views, wwwroot      vues Razor, CSS, JS, polices et photos des produits
```
