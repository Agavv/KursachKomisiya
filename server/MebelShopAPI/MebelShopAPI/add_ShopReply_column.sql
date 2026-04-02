-- =====================================================================
-- Migration: Add ShopReply column to Reviews table
-- Run this script once on your MebeliBD database before starting the API
-- =====================================================================

-- Add ShopReply column if it doesn't already exist
IF NOT EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Reviews'
      AND COLUMN_NAME = 'ShopReply'
)
BEGIN
    ALTER TABLE Reviews
    ADD ShopReply VARCHAR(MAX) NULL;

    PRINT 'Column ShopReply added to Reviews table.';
END
ELSE
BEGIN
    PRINT 'Column ShopReply already exists — no changes made.';
END
GO

-- Drop the old stored procedure if it exists (replaced by direct EF update)
IF OBJECT_ID('dbo.sp_AddOrUpdateShopReplyToReview', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_AddOrUpdateShopReplyToReview;
    PRINT 'Old stored procedure sp_AddOrUpdateShopReplyToReview dropped.';
END
GO
