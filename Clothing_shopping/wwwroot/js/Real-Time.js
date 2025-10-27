const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .build();

connection.on("ReceiveNotification", function (message) {
    const container = document.querySelector(".Notification_Notification");
    if (container) {
        const item = document.createElement("div");
        item.className = "notification-item";
        item.innerHTML = `
                <div class="alert alert-info mt-1 mb-1" style="font-size: 14px;">
                    ${message}
                </div>`;
        container.prepend(item); // thêm lên đầu danh sách
    }
});

connection.start().catch(err => console.error(err));