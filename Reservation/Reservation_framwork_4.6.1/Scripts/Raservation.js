function changChild(max, p) {
    $('#child').html('0');
    $('#child_ul').html('');
    for (var i = 0; i < max - p + 1; i++) {
        $('#child_ul').append('<li class="select_data" value="' + i + '">' + i + '</li>')
    }

    $('.child_hide_select li').on('click', function (e) {
        $('#child').text(this.innerHTML);
    });
}

//依據寬度不同啟動js，以下是當卷軸到70的時候幫選單固定
function WinOnResize() { // 以 function 的方式來寫
    if (document.body.clientWidth >= 500) // 當瀏覽器寬度 > 500px 例如 Full-HD
    {
        $(window).scroll(function () {
            if ($(this).scrollTop() > 70) {                 /* 要滑動到選單的距離 */
                $('.list-menu').addClass('menuFixed');      /* 幫選單加上固定效果 */
            } else {
                $('.list-menu').removeClass('menuFixed');   /* 移除選單固定效果 */
            }
        });
    }
}

window.onresize = WinOnResize;
window.onload = WinOnResize;

$(function () {
    $(".flexslider").flexslider({
        slideshowSpeed: 5000, //展示时间间隔ms
        animationSpeed: 500, //滚动时间ms
        touch: true, //是否支持触屏滑动,
        prevText: "",
        nextText: "",
        controlNav: false
        
    });

    //日曆中文化
    $.datepicker.regional['zh-TW'] = {
        clearText: '清除', clearStatus: '清除已選日期',
        closeText: '關閉', closeStatus: '取消選擇',
        prevText: '<上一月', prevStatus: '顯示上個月',
        nextText: '下一月>', nextStatus: '顯示下個月',
        currentText: '今天', currentStatus: '顯示本月',
        monthNames: ['一月', '二月', '三月', '四月', '五月', '六月',
            '七月', '八月', '九月', '十月', '十一月', '十二月'],
        monthNamesShort: ['一', '二', '三', '四', '五', '六',
            '七', '八', '九', '十', '十一', '十二'],
        monthStatus: '選擇月份', yearStatus: '選擇年份',
        weekHeader: '周', weekStatus: '',
        dayNames: ['星期日', '星期一', '星期二', '星期三', '星期四', '星期五', '星期六'],
        dayNamesShort: ['周日', '周一', '周二', '周三', '周四', '周五', '周六'],
        dayNamesMin: ['日', '一', '二', '三', '四', '五', '六'],
        dayStatus: '設定每周第一天', dateStatus: '選擇 m月 d日, DD',
        dateFormat: 'yy-mm-dd', firstDay: 1,
        initStatus: '請選擇日期', isRTL: false
    };
    $("#datepicker").datepicker();
    $.datepicker.setDefaults($.datepicker.regional['zh-TW']);

    $('[data-record]').fancybox({
        toolbar: false,
        smallBtn: true,
        clickSlide: 'false',
        touch: {
            vertical: true,  // 允許垂直方向拖拽
            momentum: true   // Continue movement after releasing mouse/touch when panning
        },
        iframe: {
            preload: false,
            css: {
            },
            slideShow:
            {
                autoStart: false,
                speed: 4000,
            },
            clickOutside: 'close',
        },
    });

    // 連結按鈕提示框
    $('[data-toggle="tooltip"]').tooltip();

    $('.reservation_btm .icon-button').click(function () {
        $('.reservation_btm .icon-button').eq($(this).index()).addClass("filter").siblings().removeClass("filter");
    });

    //UI下拉式選單 包含點選其他地方收合下拉式
    $('.people_hide_select, .child_hide_select').on('click', function (e) {
        $(this).children().toggleClass('switchDisplay');
        //e.stopPropagation();  //停止後續DOM事件
    });

    //將點選的值 丟到上面
    $('.people_hide_select li').on('click', function () {
        $('#people').text(this.innerHTML);
    });

    //將點選的值 丟到上面
    $('.child_hide_select li').on('click', function () {
        $('#child').text(this.innerHTML);
    });

    $('.member_bg .member_block').on('click', function (e) {
        if ($(this).hasClass("switchDisplay") === false) {
            $('.member_bg .member_block').removeClass('switchDisplay');
        }
        $(this).siblings().toggleClass('switchDisplay');
        //e.stopPropagation();
    });

    //UI下拉式選單 包含點選其他地方收合下拉式
    $('.county_bg').on('click', function () {
        if ($('.county_bg').hasClass("switchDisplay") === false) {
            $('.county_bg ul').removeClass('switchDisplay');
        }
        $('.county_bg').siblings().toggleClass('switchDisplay');
    });

    //將點選的值 丟到上面
    $('.county_bg ul li').on('click', function () {
        var people_lis = document.getElementById("county_ul").getElementsByTagName("li");
        for (i = 0; i < people_lis.length; i++) {
            $('#county').text(this.innerHTML);
        }
    });

    // UI下拉式選單 包含點選其他地方收合下拉式
    $('.dist_bg').on('click', function () {
        if ($(this).hasClass("switchDisplay") === false) {
            $('.dist_bg ul').removeClass('switchDisplay');
        }
        $(this).toggleClass('switchDisplay').siblings().toggleClass('switchDisplay');
    });

    // 將點選的值 丟到上面
    $('.dist_bg ul li').on('click', function () {
        var people_lis = document.getElementById("dist_ul").getElementsByTagName("li");
        for (i = 0; i < people_lis.length; i++) {
            $('#dist').text(this.innerHTML);
        }
    });

    // 點選其他地方收合下拉式
    $(document).click(function (e) {
        if ($(e.target).parents('.child_hide_select, .people_hide_select, .gender_hide_select, .county_bg, .dist_bg').length === 0) {
            $('.switchDisplay').removeClass('switchDisplay');
        } else {
            var target = $(e.target);
            if (!target.hasClass('switchDisplay'))
                target = $(e.target).parents('.switchDisplay');

            $('.switchDisplay').not(target.siblings('.switchDisplay')).removeClass('switchDisplay');
        }
    });
});