// index
var swiper = new Swiper(".mySwiper", {
    direction: "vertical",
    pagination: {
        el: ".swiper-pagination",
        clickable: true,
    },
    mousewheel: true,
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

// user drop down
document.getElementById("user_drop_down").addEventListener("click", () => {
    document.getElementById("user_profile").classList.toggle("ac");
})

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

    //const startYear = 1900;
    //const currentYear = new Date().getFullYear();
    //for (let yr = startYear; yr <= currentYear; yr++) {
    //    let option = document.createElement("option");
    //    option.value = yr;
    //    option.textContent = yr;
    //    year.appendChild(option);
    //}
    //for (let m = 1; m <= 12; m++) {
    //    let option = document.createElement("option");
    //    option.value = m;
    //    option.textContent = m;
    //    month.appendChild(option);
    //}
    //for (let d = 1; d <= 31; d++) {
    //    let option = document.createElement("option");
    //    option.value = d;
    //    option.textContent = d;
    //    day.appendChild(option);
    //}

}