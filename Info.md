---

---

# Authentication schemes

## JWT Token

* [See more: OAuth 2.0 for Browser-Based Apps](https://www.ietf.org/archive/id/draft-ietf-oauth-browser-based-apps-14.html#name-cookies)

* Web apps should use redirect-based flow

* Mobile apps should use JWT tokens

* Web apps should use cookie

* Strong protection mechanisms should be put in place

* Need to be manually added to the http request headers

##### Advantages

- Refresh tokens - the JWT tokens could be set to a lower lifetime and use a refresh token to get a new JWT token (that should also be limited in life)
- Stateless Authentication - enables scaling and easier load balancing, the server does not have to keep track of the session status for each user
- Support - many already existing libraries support working with JWTs
- Authorization - the server decides in the JWTs what each user can do what, like user roles and permissions
- CORS - for communication across different origins

##### Disatvantages

* Need to be manually added to the http request headers
* Security risk - vulnerable to XSS attacks, token exfiltration, redirect-based attacks (when using redirect-based flow, which is required for web apps)
* Size - JWTs are generally larger, containing all the claims relevant to the user
* Absence of revocation on server side - there's no proper revocation mechanism for JWT tokens (except keeping a blacklist registry or having a very short lifetime)
* Restricted updates - JWT tokens cannot typically be updated after being emitted, like updated roles or permissions, without having the user sign in again

##### Recommendations

* Limit the scopes to the minimum the user needs
* Implement HTTPS only requirement
* Use CSRF defences, by either of the following:  
  * Ensuring the authorization server supports PKCE
  * By using the OAuth 2.0 "state" parameter or the OpenID Connect "nonce" parameter to carry one-time use CSRF tokens
* Storing the token (in order of security):  
  * Should ideally be in a Service Worker, that intercepts the application requests and handles authentication and appending the proper headers automatically. The web worker could be closed unexpectedly by the browser, so that should be taken into consideration.  
  * Should be stored in memory only, in a clojure variable rather than object property  
  * Stored in encrypted format using WebCrypto API 
  * Stored in a cookie / local storage / session storage or IndexedDB - easy to steal from JS or from the underlying filesystem
* If the IdP supports this, might be worth using a sender-constrained token  
  * A mechanism for this would pe [DPoP (Demonstrating Proof-of-Possession)](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-dpop)
  * A private key should be used by the client, the public key should be included in the DPoP header, to prove the ownership of the JWT/refresh tokens
  * The storage considerations move to securing the private key, similar to the issues with storing the JWT
  * The advantage is that if a malicious actor steals the JWT token, he wouldn't be able to do anything without the private key, it makes impersonation a bit more difficult

---

## Cookies

##### Advantages

* Simple instalation and maintenance, handled automatically by the browser
* Are automatically sent to the server on each request
* The authentication is stored server-side (stateful server), which makes revoking a user session easy

##### Disatvantages

* Cookies are always sent with the requests, even when you don't want to
* Do now work for cross-origin authentication or SSO

##### Recommendations

* Use only HttpOnly, Secure and SameSite strict cookies. 
  * HttpOnly cookies are innacessible from the JS code, mitigating XSS attacks.  
  * SameSite asserts will only be sent for same-site requests (requests originating from the same site that set the cookie). Mitigates CSRF attacks. 
  * Secure cookies are only sent with an encrypted HTTPS request. Helps mitigate MitM (Man-in-the-Middle) attacks.
* Don't store sensitive data in cookies, cookies are not encrypted.

---

### Browser-based applications

* Browser-based applications are considered very insecure

##### Process for obtaining a JWT token

* 

---

### Native Applications

* See more: [OAuth 2.0 for native apps](https://datatracker.ietf.org/doc/html/rfc8252) and [OAuth 2.0 for First-Party Applications](https://datatracker.ietf.org/doc/draft-parecki-oauth-first-party-apps/)
* For a good experience, mobile apps should not use redirect-based flow
* A native application might instead prefer to use Single-Sign-On, instead of short lived tokens

##### Process for obtaining a JWT token using the browser ***[Recommended]***

* This process is similar to the way of obtaining a JWT token in a browser-based app
* The advantage of this process is that the autorization requests that use the browser are more secure and can take advantage of the users authentication state. This enables the use the authentication session in the browser to enable single sign-on, as users don't need to authenticate to the authorization server each time they use a new app.
* Recommended to use the system browser instead of a browser window embeded in the application, offers better security.
1. Client app opens a browser tab with the authorization request.
2. Authorization endpoint receives the authorization request, authenticates the user, and obtains authorization. Authenticating the user may involve chaining to other authentication systems.
3. Authorization server issues an authorization code to the redirect URI.
4. Client receives the authorization code from the redirect URI.
5. Client app presents the authorization code at the token endpoint.
6. Token endpoint validates the authorization code and issues the tokens requested.

<img src=".\md-resources\native-with-browser.drawio.svg" title="" alt="drawio" data-align="center">

##### Process for obtaining a JWT token without using the browser

* The client makes a POST request to the authorization challenge endpoint, sending some info, such as username
* The authorization server determines if the info received is enough and
  * Either reponds with an error, requesting additional info, to which the client must respond with the requested info. This back and forth can happen multiple times
  * Either it returns an authorization code
* The client sends the authorization code received to obtain a token from the Token endpoint
* The authorization server sends an Access Token

##### Process for implementing SSO

* 

---

> The way i did it is that the user gets tokens whenever they do a request. so every request uses the tokens from the previous request and then get replaced with the tokens from the current request, so they are ready for the next request. the tokens are only valid for 1 request so thats why every request must change the tokens.
