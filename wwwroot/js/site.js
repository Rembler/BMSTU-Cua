// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function addUser(id) {

    $.post("/Room/AddUser", { id: id })
        .done(function (data) {
            
            if (data != null) {
                var result_str = "<p>"+data+"</p>";
                console.log(data);

                // $(`#users-list-${id}`).html(result_str);
                $(`#users-list-${id}`).append(result_str);
                console.log(data);
            }
        });
}