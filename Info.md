# Authentication schemes

## JWT Token

* [See more: OAuth 2.0 for Browser-Based Applications](https://www.ietf.org/archive/id/draft-ietf-oauth-browser-based-apps-14.html).
* Web apps should use redirect-based flow.
* Strong protection mechanisms should be put in place.
* Need to be manually added to the HTTP request headers.

##### Advantages

- Refresh tokens - the JWT tokens could be set to a lower lifetime and use a refresh token to get a new JWT token (that should also be limited in life).
- Stateless Authentication - enables scaling and easier load balancing, the server does not have to keep track of the session status for each user
- Support - many already existing libraries support working with JWTs.
- Authorization - the server decides in the JWTs what each user can do, like user roles and permissions.
- CORS - for communication across different origins.

##### Disatvantages

* Need to be manually added to the HTTP request headers.
* Security risk - vulnerable to XSS attacks, token exfiltration, and redirect-based attacks (when using redirect-based flow, which is required for web applications).
* Size - JWTs are generally larger, containing all the claims relevant to the user.
* Absence of revocation on server side - there's no proper revocation mechanism for JWT tokens (except keeping a blacklist registry or having a very short lifetime).
* Restricted updates - JWT tokens cannot typically be updated after being emitted, like updated roles or permissions, without having the user sign in again.

##### Recommendations

* Limit the scopes to the minimum the user needs.
* Implement HTTPS-only requirement.
* Use CSRF defences by doing either of the following:  
  * Ensuring the authorization server supports PKCE.
  * By using the OAuth 2.0 "state" parameter or the OpenID Connect "nonce" parameter to carry one-time use CSRF tokens.
* Storing the token (in order of security):  
  * Should ideally be in a Service Worker, that intercepts the application requests and handles authentication and appending the proper headers automatically. The web worker could be closed unexpectedly by the browser, so that should be taken into consideration.  
  * Should be stored in memory only, in a clojure variable rather than an object property.
  * Stored in encrypted format using WebCrypto API.
  * Stored in a cookie / local storage / session storage, or IndexedDB - easy to steal from JS or from the underlying filesystem.
* If the IdP supports this, it might be worth using a sender-constrained token.
  * A mechanism for this would be [DPoP (Demonstrating Proof-of-Possession)](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-dpop).
  * A private key should be used by the client; the public key should be included in the DPoP header, to prove the ownership of the JWT/refresh tokens.
  * The storage considerations move to securing the private key, similar to the issues with storing the JWT.
  * The advantage is that if a malicious actor steals the JWT token, the actor wouldn't be able to do anything without the private key, which makes impersonation a bit more difficult.

---

## Cookies

##### Advantages

* Simple installation and maintenance, handled automatically by the browser.
* Are automatically sent to the server on each request.
* The authentication is stored server-side (stateful server), which makes revoking a user session easy.

##### Disatvantages

* Cookies are always sent with the requests, even when you don't want to.
* Do not work for cross-origin authentication or SSO.

##### Recommendations

* Use only HttpOnly, Secure, and SameSite strict cookies. 
  * HttpOnly cookies are inaccessible from the JS code, mitigating XSS attacks.  
  * SameSite asserts will only be sent for same-site requests (requests originating from the same site that set the cookie). Mitigates CSRF attacks. 
  * Secure cookies are only sent with an encrypted HTTPS request. Helps mitigate MitM (Man-in-the-Middle) attacks.
* Don't store sensitive data in cookies, cookies are not encrypted.

---

# Implementation

## Browser-based applications

* See more: [OAuth 2.0 for Browser-Based Applications](https://www.ietf.org/archive/id/draft-ietf-oauth-browser-based-apps-14.html).
* Browser-based applications are and should be considered very insecure.
* The application is considered a public client, since there is no way to provision it a client secret in this model.
* Must implement PKCE when obtaining an access token, and authorization servers must support and enforce PKCE for such clients.
* Must register one or more redirect URIs with the authorization server, and use only exact registered redirect URIs in the authorization server request.

##### Obtain a JWT token directly

* This is the least secure way of obtaining a JWT token.
* Besides the general risks of XSS, if tokens are stored or handled directly by the browser, XSS poses an additional risk of token exfiltration. 
* The application is responsible for storing the access token (and optional refresh token) as securely as possible using the appropriate browser APIs.
* The Autorization Server and Resource Server must support the necessary CORS headers.

<img title="browser-based application" src="./md-resources/browser-based-directly.drawio.svg" alt="browser-based application" data-align="center">

1. The JS code is first loaded from a static web host into the browser **(A)**. 
2. The code in the browser initiates the Authorization Code flow with the PKCE extension **(B)**.
3. The application obtains an access token via a POST request **(C)**.
4. When the application whats to make a request to the Resource Server, it can interact with the Resource Server directly. It includes an access token in the request **(D)** and receives the Resource Server's response **(E)**.
   
   

##### Obtain a JWT using Service Worker

* This is a more secure method of obtaining and storing the JWT tokens than obtaining them directly in the browser.
* Service Workers are run in a separate context from the DOM, have no access to the DOM, and the DOM has no access to the Service Worker or the memory of the Service Worker.
* The Service Worker is the most secure place in browser to acquire and store tokens, as an XSS attack would be unable to exfiltrate the tokens.
* If you store the tokens in the Service Worker memory (there's no filesystem API for Service Worker yet), the Service Worker [might be killed by the browser in some situations](https://www.w3.org/TR/service-workers/#service-worker-lifetime), so that case needs to be taken into account. 
* In this architecture, a Service Worker intercepts calls from the frontend to the resource server. As such, it completely isolates calls to the authorization server from XSS attack surface, as all tokens are safely kept in the service worker context without any access from other JavaScript contexts. The service worker is then solely responsible for adding the token in the authorization header to calls to the resource server.

<img title="browser-based application using service worker" src="./md-resources/browser-based-service-worker.drawio.svg" alt="browser-based application using service worker" data-align="center">

###### Implementation Guidelines

* The Service Worker must initiate the OAuth 2.0 Authorization Code grant with PKCE itself.
* The Service Worker must intercept the authorization code when the Authorization Server redirects to the application.
* The Service Worker must then initiate the token request itself.
* The Service Worker must not transmit tokens, authorization codes or PKCE code verifier to the application.
* The Service Worker must block authorization requests and token requests initiating from the application in order to avoid any front-end side-channel for getting tokens. The only way of starting the authorization flow should be through the service worker. This protects against re-authorization from XSS-injected code.
* The user must register the Service Worker before running any code interacting with the user.
  
  

##### Obtain a JWT using Backend-For-Frontend (BFF) Proxy

* In this scenario, the connection between the browser and the BFF proxy should be a session cookie provided by the BFF proxy.
* While the security of this model is strong, since the OAuth tokens are never sent to the browser, there are performance and scalability implications of deploying a BFF proxy server and routing all JS requests through the server.
* *[TODO]*
  
  
  
  

##### Obtain a JWT using Token-Mediating Backend

* This is an alternative to a ful BFF, where all all resource requests are done directly through the browser, the token-mediating backend only handles obtaining the tokens and forwards them to the browser.
* *[TODO]*
  
  

## Native Applications

* For a good experience, mobile apps should not use redirect-based flow, although redirect-based flow has some security and convenience advantages.
* A native application might instead prefer to use Single Sign-On, instead of short lived tokens.

##### Obtain a JWT token using the browser ***[Recommended]***

* See more: [OAuth 2.0 for native applications](https://datatracker.ietf.org/doc/html/rfc8252).
* This process is similar to the way of obtaining a JWT token in a browser-based application.
* The advantage of this process is that the authorization requests that use the browser are more secure and can take advantage of the user's authentication state. This enables the use of the authentication session in the browser to enable single sign-on, as users don't need to authenticate to the authorization server each time they use a new app.
* It is recommended to use the system browser instead of a browser window embedded in the application, it offers better security.

<img title="native application using browser flowchart" src="./md-resources/native-with-browser.drawio.svg" alt="native application using browser flowchart" data-align="center">

1. The Client app opens a browser tab with the authorization request.
2. The authorization endpoint receives the authorization request, authenticates the user, and obtains authorization. Authenticating the user may involve chaining to other authentication systems.
3. The authorization server issues an authorization code to the redirect URI.
4. The client receives the authorization code from the redirect URI.
5. The client app presents the authorization code at the token endpoint.
6. The token endpoint validates the authorization code and issues the tokens requested.
   
   

##### Obtain a JWT token without using the browser

* See more: [OAuth 2.0 for First-Party Applications](https://datatracker.ietf.org/doc/draft-parecki-oauth-first-party-apps/).
* The client makes a POST request to the authorization challenge endpoint, sending some info, such as username.
* The authorization server determines if the information received is enough and:
  * Either reponds with an error, requesting additional info, to which the client must respond with the requested info. This back and forth can happen multiple times.
  * Either it returns an authorization code.
* The client sends the authorization code received to obtain a token from the Token endpoint.
* The authorization server sends an Access Token.

<img title="native application without using browser flowchart" src="./md-resources/native-without-browser.drawio.svg" alt="native application without using browser flowchart" data-align="center">

1. The first-party client starts the flow, by presenting the user with a "sign in" button, or collecting information from the user, such as their email address or username.
2. The client initiates the authorization request by making a POST request to the Authorization Challenge Endpoint, optionally with information collected from the user (e.g., email or username).
3. The authorization server determines whether the information provided to the Authorization Challenge Endpoint is sufficient to grant authorization, and either responds with an authorization code or responds with an error. In this example, it determines that additional information is needed and responds with an error. 
   The error may contain additional information to guide the Client on what information to collect next. This pattern of collecting information, submitting it to the Authorization Challenge Endpoint and then receiving an error or authorization code may repeat several times.
4. The client gathers additional information (e.g., signed passkey challenge or one-time code from email) and makes a POST request to the Authorization Challenge Endpoint.
5. The Authorization Challenge Endpoint returns an authorization code.
6. The client sends the authorization code received in step **(5)** to obtain a token from the Token Endpoint.
7. The Authorization Server returns an Access Token from the Token Endpoint.
   
   

##### Single Sign-On (SSO)

* *[TODO]*
  
  

## Input constrained devices

* This refers to internet-connected devices that either lack a browser or are input constrained (like smart TVs, media consoles, digital picture frames, printers, etc.).
* These devices should use **Device Authorization Grant** to obtain a JWT token.
* Instead of interacting directly with the end user's user agent (i.e., browser), the device client instructs the end user to use another computer or device and connect to the authorization server to approve the access request.
* Since the protocol supports clients that can't receive incoming requests, clients poll the authorization server repeatedly until the end user completes the approval process.
* Even if the device displays a QR code to be scanned by the user device (e.g., smartphone), it is recommended to still display the verification URI for users who are not able to scan the QR code.
* Due to the polling nature of this protocol, care is needed to avoid overloading the capacity of the token endpoint.
* The client should only start a device authorization request when prompted by the user and not automatically, such as when the app starts or when the previous authorization session expires or fails.

##### Requirements

* The device is already connected to the Internet.
* The device is able to make outbound HTTPS requests.
* The device is able to display or otherwise communicate a URI and code sequence to the user.
* The user has a secondary device (e.g., personal computer or mobile phone) from which they can process the request.

##### Obtain a JWT token

* See more: [OAuth 2.0 Device Authorization Grant](https://datatracker.ietf.org/doc/rfc8628/).
* The client initiates the authorization flow by requesting a set of verification codes from the authorization server with an HTTP POST request to the authorization endpoint. The client includes in the request the client ID and the scope.
* The authorization server generates a unique device verification code and an end-user code, that are valid for a limited time, as well as the verification URI to be visited by the user on their secondary device (such as in a browser or on their mobile phone).
* The client displays the user code and verification URI and instructs the user to visit the URI and enter the user code.
* During this time, the client continuously polls the token endpoint with the device code until the user completes the interaction, the code expires, or another error occurs.
* After displaying instructions to the user, the client creates an access token request and sends it to the token endpoint.
* While polling, the server might return an error:
  * Authorization pending: the authorization request is still pending for the user to complete all the steps on the secondary device.
  * Slow down: the authorization request is still pending, but the polling interval must be increased by 5 seconds for this and all subsequent requests.
  * Access denied: the authorization request was denied.
  * Expired token: the device code has expired and the authorization session must be concluded. The client may commence a new device authorization request but should wait for user interaction before restarting, to avoid unnecessary polling.

<img title="input constrained devices flowchart" src="./md-resources/input-constrained-devices.drawio.svg" alt="input constrained devices flowchart" data-align="center">

1. The client requests access from the authorization server and includes its client identifier in the request.
2. The authorization server issues a device code and an end-user code and provides the end-user verification URI.
3. The client instructs the end user to use a user agent on another device and visit the provided end-user verification URI. The client provides the user with the end-user code to enter in order to review the authorization request.
4. The authorization server authenticates the end user (via the user agent), and prompts the user to input the user code provided by the device client. The authorization server validates the user code provided by the user, and prompts the user to accept or decline the request.
5. While the end user reviews the client's request (**step 4**), the client repeatedly polls the authorization server to find out if the user completed the user authorization step. The client includes the device code and its client identifier.
6. The authorization server validates the device code provided by the client and responds with the access token if the client is granted access, an error if they are denied access, or an indication that the client should continue to poll.
