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
if (Controller === "User" && Action === "Login") {
    
}

//register
//if (Controller === "User" && Action === "Register") {
    
//}

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

//var search_menu = document.getElementById("search");
//var btn_search = document.getElementById("btn_search");
//btn_search.addEventListener("click", () => {
//    search_menu.classList.toggle("ac");
//    if (btn_search.classList.contains("fa-magnifying-glass")) {
//        btn_search.classList.remove("fa-magnifying-glass");
//        btn_search.classList.add("fa-x");
//        return;
//    }
//    btn_search.classList.remove("fa-x");
//    btn_search.classList.add("fa-magnifying-glass");
//})


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

$(document).ready(function () {
    var searchTimeout;
    var searchBox = $("#search");
    var searchInput = $("#search_input");
    var resultBox = $("#search_results");
    var btnSearchIcon = $("#btn_search_icon");

    // 1. Bật / Tắt khung Search (Khi bấm icon kính lúp ở menu dưới)
    $("#btn_open_search").on("click", function (e) {
        e.preventDefault();
        searchBox.toggleClass("ac"); // Toggle class hiển thị (bạn đã có CSS .search.ac { display: flex })

        if (searchBox.hasClass("ac")) {
            // Khi MỞ: Đổi icon thành X, focus vào ô nhập
            btnSearchIcon.removeClass("fa-magnifying-glass").addClass("fa-x");
            setTimeout(function () { searchInput.focus(); }, 100);
            $('body').css('overflow', 'hidden'); // Khóa cuộn trang chính
        } else {
            // Khi ĐÓNG
            closeSearch();
        }
    });

    // 2. Nút đóng nhỏ bên trong khung search (X nhỏ cạnh ô input)
    $("#btn_close_search_box").on("click", function (e) {
        e.preventDefault();
        closeSearch();
    });

    // Hàm đóng search chung
    function closeSearch() {
        searchBox.removeClass("ac");
        btnSearchIcon.removeClass("fa-x").addClass("fa-magnifying-glass"); // Đổi lại icon kính lúp
        searchInput.val(''); // Xóa chữ đã nhập
        resultBox.hide().html('');  // Xóa và ẩn kết quả
        $('body').css('overflow', 'auto'); // Mở lại cuộn trang
    }

    // 3. Sự kiện nhập liệu (LIVE SEARCH LOGIC)
    searchInput.on('keyup', function () {
        var keyword = $(this).val().trim();

        // Xóa timeout cũ (debounce) để tránh gọi API liên tục khi gõ nhanh
        clearTimeout(searchTimeout);

        if (keyword.length < 2) {
            resultBox.hide().html('');
            return;
        }

        // Delay 300ms mới gọi API
        searchTimeout = setTimeout(function () {
            $.ajax({
                url: '/Home/SearchProducts',
                type: 'GET',
                data: { keyword: keyword },
                success: function (res) {
                    if (res.success && res.data.length > 0) {
                        var html = '';
                        res.data.forEach(function (item) {
                            // Format tiền Việt Nam
                            var priceFormatted = item.price.toLocaleString('vi-VN') + ' đ';

                            // Link chi tiết sản phẩm
                            var url = `/Home/ProductDetail?TargetGroup=${item.targetGroup}&CategoryId=${item.categoryId}&ProductId=${item.productId}`;

                            html += `
                                <a href="${url}" class="search_item">
                                    <img src="${item.image}" alt="${item.name}">
                                    <div class="search_item_info">
                                        <h4>${item.name}</h4>
                                        <span>${priceFormatted}</span>
                                    </div>
                                </a>
                            `;
                        });
                        resultBox.html(html).show(); // Hiện kết quả
                    } else {
                        resultBox.html('<div class="text-center p-3 text-muted" style="padding:20px;">Không tìm thấy sản phẩm nào phù hợp.</div>').show();
                    }
                },
                error: function () {
                    console.log("Lỗi tìm kiếm");
                }
            });
        }, 300);
    });
});