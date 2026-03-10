USE master
GO

DROP DATABASE MebeliBD
GO

CREATE DATABASE MebeliBD
GO

USE MebeliBD
GO

-- Таблица ролей пользователей (director, adminstrator, manager, customer)
CREATE TABLE Roles (
	ID_Role INT PRIMARY KEY IDENTITY(1,1),
	Name VARCHAR(50) NOT NULL
);

SELECT * FROM Roles
-- Таблица пользователей
CREATE TABLE Users(
	ID_User INT PRIMARY KEY IDENTITY(1,1),
	Email VARCHAR(255) NOT NULL UNIQUE,
	PasswordHash VARCHAR(MAX) NOT NULL,
	Role_ID INT NOT NULL,
	FOREIGN KEY (Role_ID) REFERENCES Roles(ID_Role)
);

SELECT * FROM Users

UPDATE Users
SET Role_ID = 3
WHERE ID_User = 6

CREATE TABLE UserProfiles(
	User_ID INT PRIMARY KEY,
	FirstName VARCHAR(50),
	Surname VARCHAR(50),
	Theme VARCHAR(50) DEFAULT 'Light',
	FOREIGN KEY (User_ID) REFERENCES Users(ID_User)
);

-- Таблица платежных данных пользователей
CREATE TABLE UserPayments (
    User_ID INT NOT NULL,
    CardNumber NVARCHAR(64) NULL,  -- хватает на AES256 + Base64
    Expiry NVARCHAR(16) NULL,      -- AES + Base64 небольшой строки
    CVV NVARCHAR(16) NULL,         -- AES + Base64
    FOREIGN KEY (User_ID) REFERENCES Users(ID_User)
);
ALTER TABLE UserPayments
ALTER COLUMN Expiry VARCHAR(128);
ALTER TABLE UserPayments
ALTER COLUMN CVV VARCHAR(128); -- или подходящий размер


SELECT * FROM UserPayments

CREATE TABLE AuditLog (
    ID_Audit INT PRIMARY KEY IDENTITY(1,1),
    User_ID INT NULL,
    Description VARCHAR(MAX) NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (User_ID) REFERENCES Users(ID_User)
);

-- Таблица кодов подтверждения email
CREATE TABLE EmailCodes (
	ID_Code INT PRIMARY KEY IDENTITY(1,1),
	User_ID INT NULL,
	FOREIGN KEY (User_ID) REFERENCES Users(ID_User),
	Email VARCHAR(50) NOT NULL,
	Code VARCHAR(10) NOT NULL,
	Purpose VARCHAR(50) NOT NULL,
	ExpirationTime DATETIME NOT NULL,
	IsUsed BIT DEFAULT 0, 
	CreatedAt DATETIME DEFAULT GETDATE()
);

SELECT * FROM Categories
-- Таблица категорий товаров
CREATE TABLE Categories (
	ID_Category INT PRIMARY KEY IDENTITY(1,1),
	NameCategory VARCHAR(100) NOT NULL
);

-- Таблица товаров
CREATE TABLE Products (
	ID_Product INT PRIMARY KEY IDENTITY(1,1),
	ProductName VARCHAR(255) NOT NULL,
	Description VARCHAR(MAX) NOT NULL,
	Price DECIMAL(18, 2) NOT NULL,
	StockQuantity INT NOT NULL,
	Category_ID INT NOT NULL,
	FOREIGN KEY (Category_ID) REFERENCES Categories(ID_Category)
);

-- Изображения товаров
CREATE TABLE ProductImages (
	ID_Image INT PRIMARY KEY IDENTITY(1,1),
	Product_ID INT NOT NULL,
	FOREIGN KEY (Product_ID) REFERENCES Products(ID_Product),
	ImageURL VARCHAR(MAX) NOT NULL,
	IsMain BIT DEFAULT 0
);

-- Характеристики, применимые к категории товаров
CREATE TABLE Characteristics (
	ID_Characteristic INT PRIMARY KEY IDENTITY(1,1),
    Name VARCHAR(255) NOT NULL,
    Category_ID INT NOT NULL,
	ValueType VARCHAR(20) NOT NULL DEFAULT 'list', --(range или list)
    FOREIGN KEY (Category_ID) REFERENCES Categories(ID_Category)
);

-- Значения характеристик для конкретных товаров
CREATE TABLE ProductCharacteristics (
    ID_ProductCharacteristic INT PRIMARY KEY IDENTITY(1,1),
    Product_ID INT NOT NULL,
    Characteristic_ID INT NOT NULL,
    Value VARCHAR(255) NOT NULL,
    FOREIGN KEY (Product_ID) REFERENCES Products(ID_Product),
    FOREIGN KEY (Characteristic_ID) REFERENCES Characteristics(ID_Characteristic)
);

-- Отзывы на товары
CREATE TABLE Reviews (
    ID_Review INT PRIMARY KEY IDENTITY(1,1),
    Product_ID INT NOT NULL,
    User_ID INT NOT NULL,
    Rating INT NOT NULL CHECK (Rating >= 0 AND Rating <= 5),
    ReviewText VARCHAR(MAX),
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (Product_ID) REFERENCES Products(ID_Product),
    FOREIGN KEY (User_ID) REFERENCES Users(ID_User)
);

-- Товары в корзине пользователя
CREATE TABLE CartItems (
    User_ID INT NOT NULL,
    Product_ID INT NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    PRIMARY KEY (User_ID, Product_ID),
    FOREIGN KEY (User_ID) REFERENCES Users(ID_User),
    FOREIGN KEY (Product_ID) REFERENCES Products(ID_Product)
);

-- Заказы пользователей
CREATE TABLE Orders (
	ID_Order INT PRIMARY KEY IDENTITY(1,1),
	User_ID INT NOT NULL,
	FOREIGN KEY (User_ID) REFERENCES Users(ID_User),
	CreatedAt DATETIME DEFAULT GETDATE(),
	Status VARCHAR(50) DEFAULT 'Создан', --(Создан, Отправлен, Получен, Отменен)
	DeliveryType VARCHAR(50) NOT NULL,      -- Самовывоз / Доставка по адресу
    DeliveryAddress NVARCHAR(255) NOT NULL, -- Полный адрес, если доставка
    PaymentType VARCHAR(50) NOT NULL       -- Онлайн / Оплата при получении
);

SELECT * FROM Orders

-- Позиции заказа (товары в заказе)
CREATE TABLE OrderItems (
	ID_OrderItem INT PRIMARY KEY IDENTITY(1,1),
	Order_ID INT NOT NULL,
	FOREIGN KEY (Order_ID) REFERENCES Orders(ID_Order),
	Product_ID INT NOT NULL,
	FOREIGN KEY (Product_ID) REFERENCES Products (ID_Product),
	Quantity INT NOT NULL CHECK(Quantity > 0),
	UnitPrice DECIMAL(18,2) NOT NULL
);

INSERT INTO Roles (Name) VALUES 
('Директор'), 
('Администратор'), 
('Менеджер'), 
('Покупатель');

INSERT INTO Categories (NameCategory) VALUES
('Диваны'),
('Кровати');
SELECT * FROM Categories

INSERT INTO Products (ProductName, Description, Price, StockQuantity, Category_ID) VALUES
('Диван угловой "Комфорт"', 'Угловой диван с мягкой обивкой и ящиком для хранения', 1200.00, 5, 1),
('Кровать двуспальная "Соната"', 'Деревянная кровать 160x200 см', 950.00, 8, 2);

SELECT * FROM Products

INSERT INTO ProductImages (Product_ID, ImageURL, IsMain) VALUES
(1, 'divan_komfort_main.jpg', 1),
(1, 'divan_komfort_side.jpg', 0);

DELETE ProductImages

SELECT * FROM ProductImages

UPDATE ProductImages
SET ImageURL = 'divan_komfort_main.jpg'
WHERE ID_Image = 3

INSERT INTO Characteristics (Name, Category_ID, ValueType) VALUES
('Материал', 1, 'list'),
('Цвет', 1, 'list');
SELECT * FROM Characteristics

INSERT INTO ProductCharacteristics (Product_ID, Characteristic_ID, Value) VALUES
(1, 1, 'Ткань'),
(1, 2, 'Серый');
SELECT * FROM ProductCharacteristics







--Три представления
CREATE VIEW vw_ProductsWithCategory AS
SELECT p.ID_Product, p.ProductName, p.Description, p.Price, p.StockQuantity, c.NameCategory
FROM Products p
JOIN Categories c ON p.Category_ID = c.ID_Category;

CREATE VIEW vw_UserFullNames AS
SELECT u.ID_User, u.Email, r.Name AS RoleName, up.FirstName + ' ' + up.Surname AS FullName
FROM Users u
JOIN Roles r ON u.Role_ID = r.ID_Role
JOIN UserProfiles up ON up.User_ID = u.ID_User;

CREATE VIEW vw_OrdersWithItems AS
SELECT o.ID_Order, o.User_ID, u.Email, oi.Product_ID, p.ProductName, oi.Quantity, oi.UnitPrice,
       oi.Quantity * oi.UnitPrice AS TotalPrice, o.Status, o.CreatedAt
FROM Orders o
JOIN OrderItems oi ON o.ID_Order = oi.Order_ID
JOIN Products p ON oi.Product_ID = p.ID_Product
JOIN Users u ON o.User_ID = u.ID_User;

CREATE OR ALTER VIEW View_Orders AS
SELECT 
    o.ID_Order AS IdOrder,
    o.User_ID AS UserId,
    u.Email,
    up.FirstName,
    up.Surname AS LastName,
    o.CreatedAt,
    o.Status,
    o.DeliveryType,
    o.DeliveryAddress,
    o.PaymentType
FROM Orders o
INNER JOIN Users u ON u.ID_User = o.User_ID
LEFT JOIN UserProfiles up ON up.User_ID = u.ID_User;



--Три процедуры
CREATE OR ALTER PROCEDURE sp_AddOrUpdateShopReplyToReview
    @ReviewID INT,
    @ShopReply VARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CurrentText VARCHAR(MAX);

    -- Проверяем, существует ли отзыв
    IF NOT EXISTS (SELECT 1 FROM Reviews WHERE ID_Review = @ReviewID)
    BEGIN
        THROW 50001, 'Отзыв с указанным ID не найден.', 1;
    END

    SELECT @CurrentText = ReviewText FROM Reviews WHERE ID_Review = @ReviewID;

    -- Если отзыва вообще нет — добавляем только ответ магазина
    IF @CurrentText IS NULL OR LTRIM(RTRIM(@CurrentText)) = ''
    BEGIN
        UPDATE Reviews
        SET ReviewText = CONCAT('Ответ магазина: ', @ShopReply)
        WHERE ID_Review = @ReviewID;
        RETURN;
    END

    -- Если уже есть "Ответ магазина", заменим старый ответ новым
    IF @CurrentText LIKE '%Ответ магазина:%'
    BEGIN
        -- Оставляем только часть до "Ответ магазина:"
        DECLARE @BaseText VARCHAR(MAX);
        SET @BaseText = LEFT(@CurrentText, CHARINDEX('Ответ магазина:', @CurrentText) - 1);

        UPDATE Reviews
        SET ReviewText = CONCAT(
            RTRIM(@BaseText),
            'Ответ магазина: ', @ShopReply
        )
        WHERE ID_Review = @ReviewID;
    END
    ELSE
    BEGIN
        -- Если "Ответ магазина" ещё не добавлялся — просто добавляем в конец
        UPDATE Reviews
        SET ReviewText = CONCAT(
            @CurrentText,
            CHAR(13), CHAR(10),
            '---', CHAR(13), CHAR(10),
            'Ответ магазина: ', @ShopReply
        )
        WHERE ID_Review = @ReviewID;
    END
END

EXEC sp_AddOrUpdateShopReplyToReview 
    @ReviewID = 17, 
    @ShopReply = 'Спасибо за отзыв! Мы рады, что товар вам подошёл';

	SELECT * FROM Reviews




CREATE PROCEDURE sp_CreateOrder
    @UserID INT,
    @ProductID INT,
    @Quantity INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE @Stock INT, @Price DECIMAL(18,2);
        SELECT @Stock = StockQuantity, @Price = Price FROM Products WHERE ID_Product = @ProductID;
        IF @Stock < @Quantity
            THROW 50000, 'Недостаточно товара на складе', 1;

        INSERT INTO Orders(User_ID) VALUES(@UserID);
        DECLARE @OrderID INT = SCOPE_IDENTITY();

        INSERT INTO OrderItems(Order_ID, Product_ID, Quantity, UnitPrice)
        VALUES(@OrderID, @ProductID, @Quantity, @Price);

        UPDATE Products SET StockQuantity = StockQuantity - @Quantity WHERE ID_Product = @ProductID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END

CREATE PROCEDURE sp_UpdateOrderStatus
    @OrderID INT,
    @Status VARCHAR(50)
AS
BEGIN
    UPDATE Orders
    SET Status = @Status
    WHERE ID_Order = @OrderID;
END

CREATE PROCEDURE sp_AddReview
    @ProductID INT,
    @UserID INT,
    @Rating INT,
    @ReviewText VARCHAR(MAX)
AS
BEGIN
    INSERT INTO Reviews(Product_ID, User_ID, Rating, ReviewText)
    VALUES(@ProductID, @UserID, @Rating, @ReviewText);
END

-- три триггера
CREATE TRIGGER trg_AuditUsers
ON Users
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- создание нового пользователя
    INSERT INTO AuditLog(User_ID, Description)
    SELECT i.ID_User, 'Создан новый пользователь: ' + i.Email
    FROM inserted i
    LEFT JOIN deleted d ON i.ID_User = d.ID_User
    WHERE d.ID_User IS NULL;

    -- обновление пользователя
    INSERT INTO AuditLog(User_ID, Description)
    SELECT i.ID_User, 'Обновлены данные пользователя: ' + i.Email
    FROM inserted i
    JOIN deleted d ON i.ID_User = d.ID_User
    WHERE EXISTS (
        SELECT i.Email, i.PasswordHash, i.Role_ID EXCEPT
        SELECT d.Email, d.PasswordHash, d.Role_ID
    );

    -- удаление пользователя
    INSERT INTO AuditLog(User_ID, Description)
    SELECT d.ID_User, 'Удалён пользователь: ' + d.Email
    FROM deleted d
    LEFT JOIN inserted i ON d.ID_User = i.ID_User
    WHERE i.ID_User IS NULL;
END

CREATE TRIGGER trg_AuditProducts
ON Products
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    --добавление товара
    INSERT INTO AuditLog(User_ID, Description)
    SELECT NULL, 'Добавлен товар: ' + CAST(i.ID_Product AS VARCHAR) + ', ' + i.ProductName
    FROM inserted i
    LEFT JOIN deleted d ON i.ID_Product = d.ID_Product
    WHERE d.ID_Product IS NULL;

    -- изменение товара
    INSERT INTO AuditLog(User_ID, Description)
    SELECT NULL, 'Изменён товар: ' + CAST(i.ID_Product AS VARCHAR) + ', ' + i.ProductName
    FROM inserted i
    JOIN deleted d ON i.ID_Product = d.ID_Product
    WHERE EXISTS (
        SELECT i.ProductName, i.Price, i.StockQuantity EXCEPT
        SELECT d.ProductName, d.Price, d.StockQuantity
    ); 

    -- удаление товара
    INSERT INTO AuditLog(User_ID, Description)
    SELECT NULL, 'Удалён товар: ' + CAST(d.ID_Product AS VARCHAR) + ', ' + d.ProductName
    FROM deleted d
    LEFT JOIN inserted i ON d.ID_Product = i.ID_Product
    WHERE i.ID_Product IS NULL;
END

CREATE TRIGGER trg_UpdateStockOnOrder
ON OrderItems
AFTER INSERT
AS
BEGIN
    UPDATE p
    SET p.StockQuantity = p.StockQuantity - i.Quantity
    FROM Products p
    JOIN inserted i ON p.ID_Product = i.Product_ID;
END

INSERT INTO Users (Email, PasswordHash, Role_ID) VALUES
('director@gmail.com', 'a4c8ca90c4641dad25e2bd2c2cda516584779cbbf5461f5b0d55cfb5f139b81d', 1),
('admin@gmail.com', 'a4c8ca90c4641dad25e2bd2c2cda516584779cbbf5461f5b0d55cfb5f139b81d', 2),
('manager@gmail.com', 'a4c8ca90c4641dad25e2bd2c2cda516584779cbbf5461f5b0d55cfb5f139b81d', 3);

SELECT * FROM Users;
SELECT * FROM Roles;



