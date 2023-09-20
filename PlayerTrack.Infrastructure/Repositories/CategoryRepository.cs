﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AutoMapper;
using Dalamud.Logging;
using Dapper;
using FluentDapperLite.Repository;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class CategoryRepository : BaseRepository
{
    public CategoryRepository(IDbConnection connection, IMapper mapper)
        : base(connection, mapper)
    {
    }

    public IEnumerable<Category>? GetAllCategories()
    {
        PluginLog.LogVerbose("Entering CategoryRepository.GetAllCategories()");
        try
        {
            const string sql = "SELECT * FROM categories";
            var categoryDTOs = this.Connection.Query<CategoryDTO>(sql).ToList();
            return categoryDTOs.Select(dto => this.Mapper.Map<Category>(dto)).ToList();
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, "Failed to get all categories.");
            return null;
        }
    }

    public int CreateCategory(Category category)
    {
        PluginLog.LogVerbose($"Entering CategoryRepository.CreateCategory(): {category.Name}");
        using var transaction = this.Connection.BeginTransaction();
        try
        {
            var categoryDTO = this.Mapper.Map<CategoryDTO>(category);
            SetCreateTimestamp(categoryDTO);
            const string insertSql = "INSERT INTO categories (name, rank, created, updated) VALUES (@name, @rank, @created, @updated)";
            this.Connection.Execute(insertSql, categoryDTO, transaction);

            const string selectByCreatedDateSql = "SELECT id FROM categories WHERE created = @created LIMIT 1";
            var newId = this.Connection.ExecuteScalar<int>(selectByCreatedDateSql, new { categoryDTO.created }, transaction);

            transaction.Commit();

            return newId;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            PluginLog.LogError(ex, "Failed to create and retrieve category based on created date.", category);
            return 0;
        }
    }

    public bool UpdateCategory(Category category)
    {
        PluginLog.LogVerbose($"Entering CategoryRepository.UpdateCategory(): {category.Name}");
        try
        {
            var categoryDTO = this.Mapper.Map<CategoryDTO>(category);
            SetUpdateTimestamp(categoryDTO);
            const string sql =
                "UPDATE categories SET name = @name, rank = @rank, updated = @updated WHERE id = @id";
            this.Connection.Execute(sql, categoryDTO);
            return true;
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, "Failed to update category.", category);
            return false;
        }
    }

    public bool DeleteCategory(int categoryId)
    {
        PluginLog.LogVerbose($"Entering CategoryRepository.DeleteCategory(): {categoryId}");
        try
        {
            const string sql = "DELETE FROM categories WHERE id = @id";
            this.Connection.Execute(sql, new { id = categoryId });
            return true;
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, $"DeleteCategory: Failed to delete category by id {categoryId}.");
            return false;
        }
    }

    public bool SaveCategories(IEnumerable<Category> categories)
    {
        PluginLog.LogVerbose("Entering CategoryRepository.SaveCategories()");
        using var transaction = this.Connection.BeginTransaction();
        try
        {
            const string sql = @"
                    INSERT INTO categories (
                        name,
                        rank,
                        created,
                        updated)
                    VALUES (
                        @name,
                        @rank,
                        @created,
                        @updated)";

            var categoryDTOs = categories.Select(this.Mapper.Map<CategoryDTO>).ToList();
            this.Connection.Execute(sql, categoryDTOs, transaction);

            transaction.Commit();

            return true;
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, "Failed to insert categories.");
            transaction.Rollback();
            return false;
        }
    }
}