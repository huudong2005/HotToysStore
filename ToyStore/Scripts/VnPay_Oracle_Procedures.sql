-- Oracle procedures for VNPAY checkout integration

-- 1. Alias SP_CreateOrder trỏ về SP_CreateOrderHeader (cùng chữ ký tham số)
CREATE OR REPLACE PROCEDURE "SP_CreateOrder" (
    p_CustomerId       IN  NUMBER,
    p_TotalAmount      IN  NUMBER,
    p_PaymentMethod    IN  VARCHAR2,
    p_DeliveryMethod   IN  VARCHAR2,
    p_Subtotal         IN  NUMBER,
    p_DiscountValue    IN  NUMBER,
    p_DiscountStrategy IN  VARCHAR2,
    p_OrderId          OUT NUMBER
) AS
BEGIN
    "SP_CreateOrderHeader"(
        p_CustomerId,
        p_TotalAmount,
        p_PaymentMethod,
        p_DeliveryMethod,
        p_Subtotal,
        p_DiscountValue,
        p_DiscountStrategy,
        p_OrderId
    );
END;
/

-- 2. Cập nhật kết quả thanh toán VNPAY từ IPN webhook
CREATE OR REPLACE PROCEDURE "SP_UpdateVnPayResult" (
    p_OrderId              IN NUMBER,
    p_VnPayTransactionNo   IN VARCHAR2,
    p_Status               IN VARCHAR2
) AS
BEGIN
    UPDATE "Orders"
    SET "Status" = p_Status
    WHERE "OrderID" = p_OrderId;

    COMMIT;
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        RAISE;
END;
/
