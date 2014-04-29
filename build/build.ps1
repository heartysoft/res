$framework = '4.0'
properties {
    $config= if($config -eq $null) {'Debug' } else {$config}
    $base_dir = resolve-path .\..
    $source_dir = "$base_dir\src"
    $tools_dir = "$base_dir\tools"
    $env = "local"
    $out_dir = "$base_dir\out\$config"
    $res_dir = "$source_dir\res"
    $res_artefacts_dir="$res_dir\Res\bin\$config"
    $resclient_artefacts_dir="$res_dir\Res.Client\bin\$config"
    $res_test_dir = "$res_dir\Res.Core.Tests\bin\$config"
    $test_results_dir="$base_dir\test-results"
    $package_dir = "$base_dir\deploy\$config"
    $test_dir = "$out_dir\tests"
}


task default -depends local

task package -depends package-server, package-client {
    rd "$package_dir\env" -recurse -force -ErrorAction SilentlyContinue  | out-null 
    mkdir "$package_dir\env" -ErrorAction SilentlyContinue  | out-null     
    echo "packaging environment variables"
    
    exec {
        copy-item $base_dir\env\* $package_dir\env\
    }
    
    echo "Server and client packaged successfully. Bye bye."
}

task local -depends prepare, tokenize, test, package-client, package-server

task clean {
    #code
    rd $res_artefacts_dir -recurse -force  -ErrorAction SilentlyContinue | out-null
    mkdir $res_artefacts_dir  -ErrorAction SilentlyContinue  | out-null
    
    #out dirs
    rd $out_dir -recurse -force  -ErrorAction SilentlyContinue | out-null
    mkdir "$out_dir\res" -ErrorAction SilentlyContinue  | out-null
    mkdir "$out_dir\res.client" -ErrorAction SilentlyContinue  | out-null
    mkdir "$test_dir\res.core.tests" -ErrorAction SilentlyContinue  | out-null
        
    #pkg dirs
    rd $package_dir -recurse -force  -ErrorAction SilentlyContinue | out-null
    mkdir "$package_dir" -ErrorAction SilentlyContinue  | out-null
}

task compile -depends clean {
    echo $config
    echo $res_artefacts_dir
    exec { msbuild $res_dir\Res.sln /t:Clean /t:Build /p:Configuration=$config /v:q /nologo }
}

task prepare -depends compile {       
    exec {
        copy-item $res_artefacts_dir\* $out_dir\res\ 
    }
        
    exec {
        copy-item $resclient_artefacts_dir\* $out_dir\res.client\
    }
    
    exec {
        copy-item $res_test_dir\* $test_dir\res.core.tests\
    }
}

task tokenize {
    $env_dir = "$base_dir\env\$env"
    #Res server
    exec {
        & "$tools_dir\config-transform\config-transform.exe" "$out_dir\res\res.exe.config" "$env_dir\res\App.$config.config"
    }
    
    #Tests       
    exec {
        & "$tools_dir\config-transform\config-transform.exe" "$test_dir\res.core.tests\res.core.tests.dll.config" "$env_dir\res.tests\App.$config.config"
    }
}

task test {    
    $testassemblies = get-childitem "$test_dir\res.core.tests" -recurse -include *tests*.dll
    mkdir $test_results_dir  -ErrorAction SilentlyContinue  | out-null
    exec { 
        & $tools_dir\NUnit2.6.3\nunit-console-x86.exe $testassemblies /nologo /nodots /xml=$test_results_dir\res.core.tests_results.xml; 
    }
}

task package-server {  
    mkdir "$package_dir\res" -ErrorAction SilentlyContinue  | out-null
    echo "Target: $package_dir\res\"
    
    exec {
        copy-item "$out_dir\res\*" "$package_dir\res\" -Exclude "logs"
    }
}

task package-client {
    mkdir "$package_dir\res.client" -ErrorAction SilentlyContinue  | out-null      
    echo "Target: $package_dir\res.client\"
    
    exec {
        copy-item "$out_dir\res.client\*" "$package_dir\res.client\"
    }

}