document.getElementById("send-message").addEventListener("click", function (event) {
    var message = document.getElementById("message").value;
    if (message != "") {
        var user = document.getElementById("current-user").textContent;
        var url = window.location.href;
        var roomId = url.substring(url.lastIndexOf('/') + 1);
        document.getElementById("message").value = "";
        connection.invoke("SendMessage", "room-" + roomId, user, message).catch(function (err) {
            return console.error(err.toString());
        });
        event.preventDefault();
    }
});