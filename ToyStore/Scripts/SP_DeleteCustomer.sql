-- Chạy script này trên Oracle (SQL Developer / SQL*Plus).
-- Tên procedure KHÔNG bọc quote → Oracle lưu là SP_DELETECUSTOMER (khớp với C#).
-- Chỉ chặn xóa khi khách đã có đơn hàng; tự xóa giỏ hàng trước khi xóa khách.
CREATE OR REPLACE PROCEDURE SP_DELETECUSTOMER (
    p_CustomerId IN NUMBER,
    p_ResultCode OUT NUMBER
) AS
    v_OrderCount    NUMBER := 0;
    v_CustomerCount NUMBER := 0;
BEGIN
    SELECT COUNT(*) INTO v_CustomerCount
    FROM "Customer"
    WHERE "CustomerID" = p_CustomerId;

    IF v_CustomerCount = 0 THEN
        p_ResultCode := 0;
        RETURN;
    END IF;

    SELECT COUNT(*) INTO v_OrderCount
    FROM "Orders"
    WHERE "CustomerID" = p_CustomerId;

    IF v_OrderCount > 0 THEN
        p_ResultCode := 2;
        RETURN;
    END IF;

    DELETE FROM "CartItem"
    WHERE "CartID" IN (
        SELECT "CartID" FROM "Cart" WHERE "CustomerID" = p_CustomerId
    );

    DELETE FROM "Cart" WHERE "CustomerID" = p_CustomerId;
    DELETE FROM "Customer" WHERE "CustomerID" = p_CustomerId;

    p_ResultCode := 1;
    COMMIT;
END;
/
