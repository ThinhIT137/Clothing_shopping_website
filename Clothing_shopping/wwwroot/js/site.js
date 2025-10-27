// index
var swiper = new Swiper(".mySwiper", {
    direction: "vertical",
    loop: true,
    pagination: {
        el: ".swiper-pagination",
        clickable: true,
    },
    mousewheel: true,
    keyboard: {
        enabled: true,
        onlyInViewport: true,
    },
    resistanceRatio: 0,
    on: {
        slideChangeTransitionStart: function () {
            allowScroll = false;
            swiper.mousewheel.disable();
            setTimeout(() => {
                allowScroll = true;
                swiper.mousewheel.enable();
            }, 1200);
        }
    },
});

// loading
window.addEventListener("beforeunload", () => {
    document.querySelector(".Loading").style.display = "flex";
});

window.addEventListener("load", () => {
    document.querySelector(".Loading").style.display = "none";
});

function lock_password(Element, class_input) {
    if (!Element || !class_input) return;

    Element.addEventListener("click", () => {
        if (Element.classList.contains("fa-lock-open")) {
            Element.classList.add("fa-lock");
            Element.classList.remove("fa-lock-open");
            class_input.setAttribute("type", "password");
            return;
        }
        Element.classList.remove("fa-lock");
        Element.classList.add("fa-lock-open");
        class_input.setAttribute("type", "text");
    });
}

// login
if (Controller === "Login" && Action === "Login") {
    const Password_login = document.getElementById("Password_login");
    const login_lock = document.querySelector(".login_lock");
    lock_password(login_lock, Password_login);
}

//register
if (Controller === "Login" && Action === "Register") {
    const Password_register = document.getElementById("password_register");
    const register_lock = document.querySelector(".register_lock");
    lock_password(register_lock, Password_register);

    const check_Password_register = document.getElementById("check_password_register");
    const check_register_lock = document.querySelector(".check_register_lock");
    lock_password(check_register_lock, check_Password_register);

    const day = document.getElementById("day");
    const month = document.getElementById("month");
    const year = document.getElementById("year");
}

// product
if (Controller === "Home" && Action === "Product") {
    const List_menu = document.getElementById("list_menu");
    const LIST = document.getElementById("LIST");
    const iconBtn_list_menu = document.getElementById("list_menu_btn");
    iconBtn_list_menu.addEventListener("click", () => {
        List_menu.classList.toggle("ac");
        if (iconBtn_list_menu.classList.contains("fa-bars")) {
            iconBtn_list_menu.classList.remove("fa-bars");
            iconBtn_list_menu.classList.add("fa-xmark");
            LIST.style.background = "red";
            //LIST.style = `
            //    background = "red"
            //`
            return;
        }
        iconBtn_list_menu.classList.remove("fa-xmark");
        iconBtn_list_menu.classList.add("fa-bars");
        LIST.style.background = "none";
        //alert("check")
    });
}

if (IsLoggedIn == true) {
    // user drop down
    document.getElementById("user_drop_down").addEventListener("click", () => {
        document.getElementById("user_profile").classList.toggle("ac");
    })

    document.getElementById("btn_Notification").addEventListener("click", () => {
        document.getElementById("Notification").classList.toggle("ac");
    })
}