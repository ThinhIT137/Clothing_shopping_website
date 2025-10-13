//const connection = new signalR.HubConnectionBuilder()
//    .widthUrl("/appHub")
//    .build();

//connection.on("UserLoggedOut", (userId) => {
//    window.location.href = "/Login/Logout";
//    console.log(`User with ID ${userId} has logged out.`);
//})

//connection.start();

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/appHub")
    .build();

connection.on("ForceLogout", () => {
    alert("Tài khoản bị khóa, đang đăng xuất...");
    window.location.href = "/Account/Logout";
});

connection.start().catch(err => console.error(err));


/*
đang fix
*/