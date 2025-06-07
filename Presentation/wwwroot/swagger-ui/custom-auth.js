(function() {
    window.onload = function() {
        setTimeout(() => {
            const token = localStorage.getItem("jwt_token"); // Load from localStorage or replace with your method

            if (token) {
                let bearerToken = "Bearer " + token;
                let auth = {};
                auth["Bearer"] = bearerToken;

                window.ui = window.ui || {};
                if (window.ui.authActions) {
                    window.ui.authActions.preAuthorizeApiKey("Bearer", bearerToken);
                }
            }
        }, 1000); // Wait for Swagger UI to load
    };
})();
