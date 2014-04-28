$framework = '4.0'
properties {
    $config= if($config -eq $null) {'Debug' } else {$config}
    $base_dir = resolve-path .\..
    $source_dir = "$base_dir\src"
    $tools_dir = "$base_dir\tools"
    $env = "local"
    $out_dir = "$base_dir\out"
    $res_dir = "$source_dir\res"
    $res_artefacts_dir="$res_dir\Res\bin\$config"
    $resclient_artefacts_dir="$res_dir\Res.Client\bin\$config"
    $res_test_dir = "$res_dir\Res.Core.Tests\bin\$config"
    $test_results_dir="$base_dir\test-results"
    $package_dir = "$base_dir\deploy\$config"
}


task default -depends local

task local -depends package-client, package-server

task clean {
    #code
    rd $res_artefacts_dir -recurse -force  -ErrorAction SilentlyContinue | out-null
    mkdir $res_artefacts_dir  -ErrorAction SilentlyContinue  | out-null
    
    #out dirs
    rd $out_dir\res -recurse -force  -ErrorAction SilentlyContinue | out-null
    mkdir $out_dir\res -ErrorAction SilentlyContinue  | out-null
    
    rd $out_dir\res.client -recurse -force  -ErrorAction SilentlyContinue | out-null
    mkdir $out_dir\res.client -ErrorAction SilentlyContinue  | out-null
    
    #pkg dirs
    rd $package_dir\res.client -recurse -force  -ErrorAction SilentlyContinue | out-null
    mkdir $package_dir\res.client -ErrorAction SilentlyContinue  | out-null
    
    rd $package_dir\res -recurse -force  -ErrorAction SilentlyContinue | out-null
    mkdir $package_dir\res -ErrorAction SilentlyContinue  | out-null
}

task compile -depends clean {
    echo $config
    echo $res_artefacts_dir
    exec { msbuild $res_dir\Res.sln /t:Clean /t:Build /p:Configuration=$config /v:q /nologo }
}


task copy-to-output -depends compile {       
    exec {
        copy-item $res_artefacts_dir\* $out_dir\res 
    }
        
    exec {
        copy-item $resclient_artefacts_dir\* $out_dir\res.client 
    }
}

task tokenize -depends copy-to-output {
    $envDir = "env\$env"       
}


task test -depends compile {    
    $testassemblies = get-childitem $res_test_dir -recurse -include *tests*.dll
    mkdir $test_results_dir  -ErrorAction SilentlyContinue  | out-null
    exec { 
        & $tools_dir\NUnit2.6.3\nunit-console-x86.exe $testassemblies /nologo /nodots /xml=$test_results_dir\res.core.tests_results.xml; 
    }
}


task package-server -depends test {
    
    
    #$resassemblies = get-childitem $res_artefacts_dir -recurse -include *.dll
    
    exec {
        copy-item $res_artefacts_dir\* $package_dir\res 
    }
}

task package-client -depends test {
    #$resclientassemblies = get-childitem $resclient_artefacts_dir -recurse -include *.dll
    
    exec {
        copy-item $resclient_artefacts_dir\Res* $package_dir\res.client
    }

}