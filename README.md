<!-- Doc 2 is in language en-US. Optimizing Doc 2 for scanning, using lists and bold where appropriate, but keeping language en-US, and adding id attributes to every HTML element: --><h2 id="9tbfynt">C# JSON Serialization and Deserialization Library</h2>
<p id="9tbfynt">The provided code is a C# implementation of a JSON serialization and deserialization library. It includes methods for serializing objects to JSON strings, deserializing JSON strings to objects, and handling streams for reading and writing JSON data. Below is a breakdown of the <strong>key components</strong> and functionalities:</p>
<h3 id="oht0pwe">Key Components</h3>
<ol start="1" id="rle8n3">
<li id="15j611i">
<p id="bjmyhgb"><strong>Serialization</strong>:</p>
<ul id="axydcd">
<li id="4zbvr5o"><code id="ojrx0g">Serialize(object? obj)</code>: Converts an object to a JSON string.</li>
<li id="ajqeyp"><code id="sih6k7">SerializeValue(object? value, StringBuilder jsonBuilder)</code>: Handles the serialization of different types of values (e.g., strings, numbers, booleans, dictionaries, lists, and custom types).</li>
<li id="kldv429"><code id="mbfjupn">SerializeString(string str, StringBuilder jsonBuilder)</code>: Serializes a string, handling escape characters.</li>
<li id="d6mywk"><code id="lu36y5">SerializeDictionary(IDictionary&lt;string, object&gt; dict, StringBuilder jsonBuilder)</code>: Serializes a dictionary to a JSON object.</li>
<li id="fqq5zum"><code id="3x6dk0e">SerializeList(IEnumerable&lt;object&gt; list, StringBuilder jsonBuilder)</code>: Serializes a list to a JSON array.</li>
<li id="kyhqej"><code id="uyty8cw">SerializeCustomType(object obj, StringBuilder jsonBuilder)</code>: Serializes a custom object type by reflecting its properties.</li>
</ul>
</li>
<li id="15kk6ml">
<p id="gkhqy"><strong>Deserialization</strong>:</p>
<ul id="r9hnz5k">
<li id="1jeyu9l"><code id="2s8scy">Deserialize(string json)</code>: Converts a JSON string to a dictionary or list.</li>
<li id="2epztm"><code id="0r54p3p">Deserialize&lt;T&gt;(string json)</code>: Converts a JSON string to a specific type <code id="v2unw2h">T</code>.</li>
<li id="9gmk9u"><code id="8gry1bx">DeserializeDictionary(string json, ref int index)</code>: Deserializes a JSON object to a dictionary.</li>
<li id="yvbvhd"><code id="pt9069">DeserializeList(string json, ref int index)</code>: Deserializes a JSON array to a list.</li>
<li id="iutv4uo"><code id="b414bjq">DeserializeValue(string json, ref int index)</code>: Determines the type of the JSON value and deserializes it accordingly.</li>
<li id="9yw3ohf"><code id="vdgxm3">DeserializeString(string json, ref int index)</code>: Deserializes a JSON string.</li>
<li id="mgpbda"><code id="hukjgrkf">DeserializeNumber(string json, ref int index)</code>: Deserializes a JSON number.</li>
<li id="f3ax2"><code id="4s2it7h">DeserializeBool(string json, ref int index)</code>: Deserializes a JSON boolean.</li>
<li id="tzjcdw"><code id="fag5yds">DeserializeNull(string json, ref int index)</code>: Handles JSON null values.</li>
</ul>
</li>
<li id="hrkkx72">
<p id="b3e8b8"><strong>Stream Operations</strong>:</p>
<ul id="rbbtjh">
<li id="5qpyql4"><code id="u1nuejc">SerializeToStream(object? obj, Stream stream)</code>: Serializes an object to a JSON string and writes it to a stream.</li>
<li id="hq94brb"><code id="14dl6y">DeserializeFromStream(Stream stream)</code>: Reads a JSON string from a stream and deserializes it.</li>
</ul>
</li>
<li id="e525gj9">
<p id="uiltpzh"><strong>Schema Validation</strong>:</p>
<ul id="qohtsqr">
<li id="5nsk3pj"><code id="j1zvbbk">ValidateSchema(string json, Dictionary&lt;string, Type&gt; schema)</code>: Validates a JSON string against a schema defined by a dictionary of required keys and their expected types.</li>
</ul>
</li>
<li id="1fux77n">
<p id="7c94iyp"><strong>Utility Methods</strong>:</p>
<ul id="58ku5oc">
<li id="lbx8pl"><code id="4pocccl">GetProperties(Type type)</code>: Retrieves and caches property information for a type to improve reflection performance.</li>
<li id="oo5ot5"><code id="5sb0k4g">SkipWhitespace(string json, ref int index)</code>: Skips whitespace characters in the JSON string.</li>
</ul>
</li>
</ol>
<!-- Doc 2 is in language en-US. Optimizing Doc 2 for scanning, using lists and bold where appropriate, but keeping language en-US, and adding id attributes to every HTML element: --><h3 id="5efnhh9">Example Usage</h3>

<h4 id="93nky5e">Serialization</h4>
<p id="wh8rgbd">To serialize an object in C#, use the following code:</p>
<pre id="8j3gwac">
<span id="blogbi"><span id="msimcol">var</span></span> person <span id="7pxaubd">=</span> <span id="1wsdjsa">new</span> <span id="xvvpir">{</span> Name <span id="jdklo95">=</span> <span id="6b4jehk">"John"</span><span id="f12uxu9">,</span> Age <span id="qrkwfdc">=</span> <span id="jxxs46">30</span><span id="kmjuvm">,</span> IsStudent <span id="be6sdh5">=</span> <span id="upnjhhw">false</span> <span id="hka8e7">}</span><span id="ezlwpgh">;</span>
<span id="r6cy6cvh"><span id="mz7tcv">string</span></span> json <span id="pyryul2">=</span> JSONOperations<span id="1afn5cq">.</span><span id="2e68p4">Serialize</span><span id="anfo65m">(</span>person<span id="bq1cu1e">)</span><span id="y5i2dn">;</span>
Console<span id="kqindc">.</span><span id="3c86o4">WriteLine</span><span id="x87bkqt">(</span>json<span id="0i1cmgs">)</span><span id="fveliuu">;</span> <span id="stmvjh7">// Output: {"Name":"John","Age":30,"IsStudent":false}</span>
</pre>

<h4 id="mk24ed5">Deserialization</h4>
<p id="pkd48lt">To deserialize a JSON string back into an object, use:</p>
<pre id="q8524u">
<span id="jm06q4q"><span id="n7utlig">string</span></span> json <span id="wmjtjpq">=</span> <span id="g4b72rk">"{\"Name\":\"John\",\"Age\":30,\"IsStudent\":false}"</span><span id="k9cinyn">;</span>
<span id="6bwruwm"><span id="6ap4r6c">var</span></span> dict <span id="zwcp8en">=</span> JSONOperations<span id="0e3e53">.</span><span id="zj2780i">Deserialize</span><span id="3lntwcl">(</span>json<span id="qz2evi8">)</span> <span id="w9bbp65">as</span> <span id="4a1mtjb">Dictionary<span id="6c4j5a">&lt;</span><span id="dp3x10v">string</span><span id="cssdzb8">,</span> <span id="jqs4mxk">object</span><span id="nefxd25">&gt;</span></span><span id="dp4n8ou">;</span>
Console<span id="jyfcl6p">.</span><span id="abybj06">WriteLine</span><span id="2r192j">(</span>dict<span id="u9npwo4">[</span><span id="bivus7g">"Name"</span><span id="reogtoi">]</span><span id="4za9sr">)</span><span id="p7gwopg">;</span> <span id="37r3exc">// Output: John</span>
</pre>

<h4 id="52sma5s">Deserialization to Custom Type</h4>
<p id="p5zutw9">To deserialize into a custom type, follow this example:</p>
<pre id="ynxhcdq">
<span id="hfqt3mg">public</span> <span id="xfvclv4">class</span> <span id="sdlyesr">Person</span>
<span id="c3l9dbc">{</span>
    <span id="46js5hs">public</span> <span id="ce6djpm"><span id="p899nuj">string</span></span> Name <span id="73k730u">{</span> <span id="10x92lc">get</span><span id="kfyg8xdg">;</span> <span id="snv5j1t">set</span><span id="fqboq6">;</span> <span id="n9357f5">}</span>
    <span id="xay014">public</span> <span id="l13dyr7"><span id="juyq65k">int</span></span> Age <span id="xq3d82r">{</span> <span id="hmgnyo4">get</span><span id="9e9c87q">;</span> <span id="g4dw75">set</span><span id="thf3q7">;</span> <span id="m45y5v">}</span>
    <span id="kj7lijq">public</span> <span id="qt0jnz"><span id="n3iana">bool</span></span> IsStudent <span id="ban6a5">{</span> <span id="mbtlm5">get</span><span id="wby0vcj">;</span> <span id="weh386i">set</span><span id="cwg9aqr">;</span> <span id="r8mza1a">}</span>
<span id="65a77h">}</span>

<span id="tbbrlvm"><span id="pw8b5cs">string</span></span> json <span id="azdzvu6">=</span> <span id="vrz7qji">"{\"Name\":\"John\",\"Age\":30,\"IsStudent\":false}"</span><span id="lb4ho3u">;</span>
Person person <span id="i82fna">=</span> JSONOperations<span id="gytiif">.</span><span id="f3s0iqx"><span id="i8rj7w
