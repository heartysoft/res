/*
 Pre-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be executed before the build script.	
 Use SQLCMD syntax to include a file in the pre-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the pre-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/

IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = '$(DatabaseName)')
Begin
	use [master]
	CREATE DATABASE [$(DatabaseName)]
	ALTER DATABASE [$(DatabaseName)] SET COMPATIBILITY_LEVEL = 100
	IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
	begin
	EXEC [$(DatabaseName)].[dbo].[sp_fulltext_database] @action = 'enable'
	end
	ALTER DATABASE [$(DatabaseName)] SET ANSI_NULL_DEFAULT OFF 
	ALTER DATABASE [$(DatabaseName)] SET ANSI_NULLS OFF 
	ALTER DATABASE [$(DatabaseName)] SET ANSI_PADDING OFF 
	ALTER DATABASE [$(DatabaseName)] SET ANSI_WARNINGS OFF 
	ALTER DATABASE [$(DatabaseName)] SET ARITHABORT OFF 
	ALTER DATABASE [$(DatabaseName)] SET AUTO_CLOSE ON 
	ALTER DATABASE [$(DatabaseName)] SET AUTO_CREATE_STATISTICS ON 
	ALTER DATABASE [$(DatabaseName)] SET AUTO_SHRINK OFF 
	ALTER DATABASE [$(DatabaseName)] SET AUTO_UPDATE_STATISTICS ON 
	ALTER DATABASE [$(DatabaseName)] SET CURSOR_CLOSE_ON_COMMIT OFF 
	ALTER DATABASE [$(DatabaseName)] SET CURSOR_DEFAULT  GLOBAL 
	ALTER DATABASE [$(DatabaseName)] SET CONCAT_NULL_YIELDS_NULL OFF 
	ALTER DATABASE [$(DatabaseName)] SET NUMERIC_ROUNDABORT OFF 
	ALTER DATABASE [$(DatabaseName)] SET QUOTED_IDENTIFIER OFF 
	ALTER DATABASE [$(DatabaseName)] SET RECURSIVE_TRIGGERS OFF 
	ALTER DATABASE [$(DatabaseName)] SET  DISABLE_BROKER 
	ALTER DATABASE [$(DatabaseName)] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
	ALTER DATABASE [$(DatabaseName)] SET DATE_CORRELATION_OPTIMIZATION OFF 
	ALTER DATABASE [$(DatabaseName)] SET TRUSTWORTHY OFF 
	ALTER DATABASE [$(DatabaseName)] SET ALLOW_SNAPSHOT_ISOLATION OFF 
	ALTER DATABASE [$(DatabaseName)] SET PARAMETERIZATION SIMPLE 
	ALTER DATABASE [$(DatabaseName)] SET READ_COMMITTED_SNAPSHOT OFF 
	ALTER DATABASE [$(DatabaseName)] SET HONOR_BROKER_PRIORITY OFF 
	ALTER DATABASE [$(DatabaseName)] SET RECOVERY SIMPLE 
	ALTER DATABASE [$(DatabaseName)] SET  MULTI_USER 
	ALTER DATABASE [$(DatabaseName)] SET PAGE_VERIFY CHECKSUM  
	ALTER DATABASE [$(DatabaseName)] SET DB_CHAINING OFF 
	USE [master]
	ALTER DATABASE [$(DatabaseName)] SET  READ_WRITE 
END
GO
USE [$(DatabaseName)]
GO

