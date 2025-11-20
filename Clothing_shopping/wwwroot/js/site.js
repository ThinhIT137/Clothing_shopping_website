var itemCart = {};
var itemOrder = {};

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

if (IsLoggedIn == true) {
    // user drop down
    document.getElementById("user_drop_down").addEventListener("click", () => {
        document.getElementById("user_profile").classList.toggle("ac");
        document.getElementById("Notification").classList.remove("ac")
    })

    document.getElementById("btn_Notification").addEventListener("click", () => {
        document.getElementById("Notification").classList.toggle("ac");
        document.getElementById("user_profile").classList.remove("ac");
    })
}

var search_menu = document.getElementById("search");
var btn_search = document.getElementById("btn_search");
btn_search.addEventListener("click", () => {
    search_menu.classList.toggle("ac");
    if (btn_search.classList.contains("fa-magnifying-glass")) {
        btn_search.classList.remove("fa-magnifying-glass");
        btn_search.classList.add("fa-x");
        return;
    }
    btn_search.classList.remove("fa-x");
    btn_search.classList.add("fa-magnifying-glass");
})

$(document).on("click", ".add-to-cartlist-btn", function (e) {
    e.preventDefault();

    var PVId = $(this).data("productVariantId");
    var QT = $(this).data("quantity");

    $.ajax({
        url: '/Order/add_cart_product',
        type: 'POST',
        data: {
            ProductVariantId: PVId, Quantity: QT
        },
        success: (res) => {
            if (res.success) {
                //alert(res.message);
            }
        }
    })
})